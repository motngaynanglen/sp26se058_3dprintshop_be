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
            .WithMessage("Loại nguồn hàng không hợp lệ, chỉ có thể là " + SourceTypes.InStock + " hoặc " + SourceTypes.PreOrder + ".");

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

    public CheckoutCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
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

        var addressExists = await _context.ShippingAddresses
            .AnyAsync(a => a.Id == request.ShippingAddressId && a.CustomerId == customer.Id, cancellationToken);
        if (!addressExists)
        {
            failures.AddFailure(nameof(request.ShippingAddressId), "Địa chỉ giao hàng không tồn tại hoặc không thuộc quyền sở hữu của bạn.");
        }

        var order = await CreateOrderAsync(customer.Id, request, failures, cancellationToken);

        failures.ThrowIfAny();

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            Order = order,
            OrderId = order.Id,
            ShippingAddressId = request.ShippingAddressId,
            ShippingFee = 0,
            ShipmentStatus = ShipmentStatuses.Preparing,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Order = order,
            OrderId = order.Id,
            InvoiceCode = $"INV-{DateTime.UtcNow.Ticks}",
            TotalAmount = order.TotalPrice,
            PaymentStatus = InvoiceStatuses.Unpaid,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };

        _context.Orders.Add(order);
        _context.Shipments.Add(shipment);
        _context.Invoices.Add(invoice);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException($"Lỗi lưu đơn hàng: {ex.InnerException?.Message}");
        }

        return _mapper.Map<OrderDTO>(order);
    }
    private async Task<Order> CreateOrderAsync(Guid customerId, CheckoutCommand request, List<ValidationFailure> failures, CancellationToken ct)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = $"ORD-{DateTime.Now:yyyyMMddHHmmss}", // Dùng UtcNow cho chuẩn xác
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
                    .FirstOrDefaultAsync(x => x.Id == itemReq.DesignVariantId.Value, ct);

                if (variant == null)
                {
                    failures.AddFailure(nameof(itemReq.DesignVariantId), $"Sản phẩm ID {itemReq.DesignVariantId} không tồn tại.");
                    continue;
                }

                if (request.SourceType == SourceTypes.InStock)
                {
                    // LUỒNG HÀNG CÓ SẴN: Phải kiểm tra và trừ kho
                    if (variant.StockQuantity < itemReq.Quantity)
                    {
                        failures.AddFailure(nameof(itemReq.Quantity), $"Sản phẩm '{variant.Name}' hiện chỉ còn {variant.StockQuantity} món, không đủ để bán sẵn.");
                    }

                    // Trừ kho
                    variant.StockQuantity -= itemReq.Quantity;

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
                        failures.AddFailure(nameof(request.SourceType), $"Sản phẩm '{variant.Name}' không hỗ trợ đặt hàng Pre-order.");
                    }

                    // KHÔNG trừ StockQuantity, KHÔNG tạo InventoryTransaction
                    // Chỉ đơn giản là ghi nhận yêu cầu vào đơn hàng
                }
                // 3. Tạo OrderItem chung cho cả 2 luồng
                if (!failures.Any(f => f.PropertyName == nameof(itemReq.Quantity) || f.PropertyName == nameof(request.SourceType)))
                {
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

        }
        return order;
    }

}


