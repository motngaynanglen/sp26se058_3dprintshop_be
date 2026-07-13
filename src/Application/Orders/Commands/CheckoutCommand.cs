using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Ocsp;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Application.Shipments;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;

[Authorize(Roles = Roles.CUSTOMER)]
public record CheckoutItemRequest
{

    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid? DesignVariantId { get; init; }
    //public Guid? DesignWorkId { get; init; }
    public int Quantity { get; init; }
}
public record CheckoutCommand : IRequest<object>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid ShippingAddressId { get; init; }
    //public Guid ShippingMethodId { get; init; }
    //[DefaultValue("ONLINE")]
    //public string? PaymentMethod { get; init; } // MoMo, BankTransfer
    [DefaultValue(SourceTypes.InStock + " hoặc " + SourceTypes.PreOrder)]
    public string SourceType { get; init; } = null!;
    public string? Note { get; init; }
    [DefaultValue(0)]
    public decimal ShippingFee { get; init; } = 0;
    [DefaultValue("MANUAL")]
    public string? ShippingCarrier { get; init; }
    public List<CheckoutItemRequest> Items { get; init; } = new();
}
public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.ShippingAddressId)
            .NotEmpty().WithMessage("Địa chỉ giao hàng không được để trống.");

        RuleFor(x => x.SourceType)
            .Must(x => x == SourceTypes.InStock || x == SourceTypes.PreOrder)
            .WithMessage("Loại nguồn hàng không hợp lệ, chỉ có thể là hàng có sẵn hoặc hàng đặt trước.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Đơn hàng phải có ít nhất một sản phẩm.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DesignVariantId).NotEmpty().WithMessage("Sản phẩm không hợp lệ.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
        });
    }
}
public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    private readonly IOrderPendingService _orderPendingService;
    private readonly ICodeGeneratorService _codeGenerator;

    public CheckoutCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user, IOrderPendingService orderPendingService, ICodeGeneratorService codeGenerator)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
        _orderPendingService = orderPendingService;
        _codeGenerator = codeGenerator;
    }
    public async Task<object> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        var userId = _user.Id.ToGuid();
        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.AccountId == userId);
        if (customer == null)
        {
            throw new ForbiddenAccessException("Chỉ có tài khoản khách hàng mới có quyền thực hiện tạo đơn hàng.");
        }
        await _orderPendingService.EnsureCustomerHasNoPendingOrderAsync(customer.Id, cancellationToken);

        var address = await _context.ShippingAddresses
            .FirstOrDefaultAsync(a => a.Id == request.ShippingAddressId && a.CustomerId == customer.Id, cancellationToken);
        if (address == null)
        {
            failures.AddFailure(nameof(request.ShippingAddressId), "Địa chỉ giao hàng không tồn tại hoặc không thuộc quyền sở hữu của bạn.");
        }

        var stockReservations = new List<StockReservation>();
        var order = await CreateOrderAsync(customer.Id, request, failures, stockReservations, cancellationToken);

        failures.ThrowIfAny();

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            Order = order,
            OrderId = order.Id,
            ShippingFee = request.ShippingFee,
            Carrier = request.ShippingCarrier ?? "MANUAL",
            ShipmentStatus = ShipmentStatuses.Preparing,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };
        ShipmentAddressSnapshot.Apply(shipment, address!);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Order = order,
            OrderId = order.Id,
            InvoiceCode = _codeGenerator.GenerateInvoiceCode(request.SourceType),
            SubTotal = order.TotalPrice,
            ShippingFee = request.ShippingFee,
            TotalAmount = order.TotalPrice + request.ShippingFee,
            PaymentStatus = InvoiceStatuses.Unpaid,
            DueDate = CoreHelper.SystemTimeNow.UtcDateTime.AddMinutes(OrderPaymentConstants.PendingPaymentLifetimeMinutes),
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        foreach (var reservation in stockReservations
            .GroupBy(x => x.DesignVariantId)
            .Select(x => new StockReservation(
                x.Key,
                x.Sum(item => item.Quantity),
                x.First().VariantName)))
        {
            var affectedRows = await _context.DesignVariants
                .Where(v => v.Id == reservation.DesignVariantId
                    && v.StockQuantity >= reservation.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(v => v.StockQuantity, v => v.StockQuantity - reservation.Quantity)
                    .SetProperty(v => v.LastModified, CoreHelper.SystemTimeNow)
                    .SetProperty(v => v.LastModifiedBy, _user.Username), cancellationToken);

            if (affectedRows == 0)
            {
                throw new BusinessException(
                    $"Sản phẩm '{reservation.VariantName}' không còn đủ tồn kho. Vui lòng kiểm tra lại giỏ hàng.",
                    ResponseCodeConstants.VAL_BUSINESS_RESTRICTION);
            }
        }

        _context.Orders.Add(order);
        _context.Shipments.Add(shipment);
        _context.Invoices.Add(invoice);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException($"Lỗi lưu đơn hàng: {ex.InnerException?.Message}");
        }

        return _mapper.Map<OrderDTO>(order);
    }
    private async Task<Order> CreateOrderAsync(
        Guid customerId,
        CheckoutCommand request,
        List<ValidationFailure> failures,
        List<StockReservation> stockReservations,
        CancellationToken ct)
    {
        var reservedQuantities = new Dictionary<Guid, int>();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = _codeGenerator.GenerateOrderCode(request.SourceType),
            CustomerId = customerId,
            OrderStatus = OrderStatuses.Pending,
            TotalPrice = 0,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
            OrderItems = new List<OrderItem>()
        };

        foreach (var itemReq in request.Items)
        {

            bool isDefaultBuy = request.SourceType == SourceTypes.InStock || request.SourceType == SourceTypes.PreOrder;
            if (isDefaultBuy && itemReq.DesignVariantId.HasValue)
            {
                // 1. Lấy thông tin biến thể sản phẩm kèm theo Lock (nếu cần)
                var variant = await _context.DesignVariants
                    .Include(x => x.DesignTemplate)
                    .FirstOrDefaultAsync(x => x.Id == itemReq.DesignVariantId.Value, ct);

                if (variant == null)
                {
                    failures.AddFailure(nameof(itemReq.DesignVariantId), $"Sản phẩm với mã {itemReq.DesignVariantId} không tồn tại.");
                    continue;
                }
                if (variant.CatalogStatus != CatalogStatuses.Published || !variant.IsActive)
                {
                    failures.AddFailure(nameof(itemReq.DesignVariantId), $"Sản phẩm '{variant.Name}' hiện chưa được mở bán.");
                    continue;
                }

                if (request.SourceType == SourceTypes.InStock)
                {
                    // LUỒNG HÀNG CÓ SẴN: Phải kiểm tra và trừ kho
                    var alreadyReserved = reservedQuantities.GetValueOrDefault(variant.Id);
                    var availableQuantity = variant.StockQuantity - alreadyReserved;
                    if (availableQuantity < itemReq.Quantity)
                    {
                        failures.AddFailure(nameof(itemReq.Quantity), $"Sản phẩm '{variant.Name}' hiện chỉ còn {availableQuantity} món, không đủ để bán sẵn.");
                        continue;
                    }

                    reservedQuantities[variant.Id] = alreadyReserved + itemReq.Quantity;
                    stockReservations.Add(new StockReservation(variant.Id, itemReq.Quantity, variant.Name));

                    // Ghi log giao dịch kho
                    var inventoryLog = new InventoryTransaction
                    {
                        Id = Guid.NewGuid(),
                        DesignVariantId = variant.Id,
                        ReferenceId = order.Id,
                        Quantity = -itemReq.Quantity,
                        Type = InventoryTransactionTypes.OrderOut,
                        Note = $"Xuất kho bán sẵn cho đơn hàng: {order.Code}",
                        Created = CoreHelper.SystemTimeNow,
                        CreatedBy = _user.Username,
                        LastModified = CoreHelper.SystemTimeNow,
                        LastModifiedBy = _user.Username
                    };
                    _context.InventoryTransactions.Add(inventoryLog);
                }
                else if (request.SourceType == SourceTypes.PreOrder)
                {
                    // LUỒNG PRE-ORDER: Kiểm tra xem biến thể này có cho phép đặt trước không
                    if (!variant.IsAllowPreOrder)
                    {
                        failures.AddFailure(nameof(request.SourceType), $"Sản phẩm '{variant.Name}' không hỗ trợ đặt hàng trước.");
                        continue;
                    }

                    // KHÔNG trừ StockQuantity, KHÔNG tạo InventoryTransaction
                    // Chỉ đơn giản là ghi nhận yêu cầu vào đơn hàng
                }
                // 3. Tạo OrderItem chung cho cả 2 luồng
                var orderItem = new OrderItem
                {
                    SourceType = request.SourceType,
                    DesignVariantId = variant.Id,
                    QuantityOrdered = itemReq.Quantity,
                    ItemName = variant.Name ?? "Sản phẩm",
                    UnitPrice = variant.Price,
                    TotalPrice = variant.Price * itemReq.Quantity,
                    FulfillmentStatus = OrderItemStatuses.Pending,
                    Order = order,
                    Created = CoreHelper.SystemTimeNow,
                    CreatedBy = _user.Username
                };
                order.OrderItems.Add(orderItem);
                order.TotalPrice += orderItem.TotalPrice;
            }

        }
        return order;
    }

    private sealed record StockReservation(Guid DesignVariantId, int Quantity, string VariantName);

}


