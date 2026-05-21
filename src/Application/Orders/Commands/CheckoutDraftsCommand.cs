using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Application.Shipments;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;

[Authorize(Roles = Roles.CUSTOMER)]
public record CheckoutDraftsItemRequest
{
    public Guid TechnicalDraftId { get; init; }
    public int Quantity { get; init; }
}
public record CheckoutDraftsCommand : IRequest<object>
{
    public Guid ShippingAddressId { get; init; }
    public string? Note { get; init; }
    public List<CheckoutDraftsItemRequest> Items { get; init; } = new();
}
public class CheckoutDraftsCommandCommandValidator : AbstractValidator<CheckoutDraftsCommand>
{
    public CheckoutDraftsCommandCommandValidator()
    {
        RuleFor(x => x.ShippingAddressId).NotEmpty().WithMessage("Địa chỉ giao hàng không được để trống.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Vui lòng chọn ít nhất một bản nháp kỹ thuật để in.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.TechnicalDraftId).NotEmpty().WithMessage("Bản nháp kỹ thuật không hợp lệ.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Số lượng in phải lớn hơn 0.");
        });
    }
}
public class CheckoutDraftsCommandCommandHandler : IRequestHandler<CheckoutDraftsCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    private readonly IOrderPendingService _orderPendingService;
    private readonly ICodeGeneratorService _codeGenerator;

    public CheckoutDraftsCommandCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user, IOrderPendingService orderPendingService, ICodeGeneratorService codeGenerator)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
        _orderPendingService = orderPendingService;
        _codeGenerator = codeGenerator;
    }
    public async Task<object> Handle(CheckoutDraftsCommand request, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();
        var userId = _user.Id.ToGuid();

        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.AccountId == userId, ct);
        if (customer == null) throw new ForbiddenAccessException("Yêu cầu tài khoản khách hàng.");
        await _orderPendingService.EnsureCustomerHasNoPendingOrderAsync(customer.Id, ct);

        var address = await _context.ShippingAddresses
            .FirstOrDefaultAsync(a => a.Id == request.ShippingAddressId && a.CustomerId == customer.Id, ct);
        if (address == null) failures.AddFailure(nameof(request.ShippingAddressId), "Địa chỉ không hợp lệ.");
        // Tạo Đơn hàng
        var order = await CreateOrderFromDraftsAsync(customer.Id, request, failures, ct);
        failures.ThrowIfAny();

        // Tạo Shipment (Giống luồng InStock của bạn)
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            Order = order,
            OrderId = order.Id,
            ShipmentStatus = ShipmentStatuses.Preparing,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username
        };
        ShipmentAddressSnapshot.Apply(shipment, address!);

        // Tạo Invoice
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Order = order,
            OrderId = order.Id,
            InvoiceCode = _codeGenerator.GenerateInvoiceCode(SourceTypes.PrintService),
            TotalAmount = order.TotalPrice,
            PaymentStatus = InvoiceStatuses.Unpaid,
            DueDate = CoreHelper.SystemTimeNow.UtcDateTime.AddMinutes(OrderPaymentConstants.PendingPaymentLifetimeMinutes),
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username
        };

        _context.Orders.Add(order);
        _context.Shipments.Add(shipment);
        _context.Invoices.Add(invoice);
        try
        {
            await _context.SaveChangesAsync(ct);

        }
        catch (Exception ex)
        {
            throw new CreateFailureException(nameof(order), ex.InnerException?.Message ??  ex.Message);
        }

        return _mapper.Map<OrderDTO>(order);
    }
    private async Task<Order> CreateOrderFromDraftsAsync(Guid customerId, CheckoutDraftsCommand request, List<ValidationFailure> failures, CancellationToken ct)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = _codeGenerator.GenerateOrderCode(SourceTypes.PrintService),
            CustomerId = customerId,
            OrderStatus = OrderStatuses.Pending,
            //OrderType = "CUSTOM_DESIGN", // Phân loại đơn thiết kế riêng
            TotalPrice = 0,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
            OrderItems = new List<OrderItem>()
        };

        foreach (var itemReq in request.Items)
        {
            // Lấy Draft kèm theo thông tin Material và DesignWork để lấy tên
            var draft = await _context.TechnicalDrafts
                .Include(d => d.DesignVersionHistory)
                    .ThenInclude(v => v.DesignWork)
                .FirstOrDefaultAsync(d => d.Id == itemReq.TechnicalDraftId, ct);

            if (draft == null)
            {
                failures.AddFailure(nameof(itemReq.TechnicalDraftId), $"Bản nháp kỹ thuật {itemReq.TechnicalDraftId} không tồn tại.");
                continue;
            }

            if (draft.DesignVersionHistory.DesignWork.CustomerId != customerId)
            {
                failures.AddFailure(nameof(itemReq.TechnicalDraftId), $"Bạn không có quyền đặt in bản nháp kỹ thuật {itemReq.TechnicalDraftId}.");
                continue;
            }

            // Lấy giá vật liệu hiện tại (Price History)
            var currentMaterialPrice = await _context.Materials.AsNoTracking()
                .Where(m => m.Id == draft.MaterialId)
                .ProjectTo<MaterialDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(ct);

            if (currentMaterialPrice == null)
            {
                failures.AddFailure(nameof(itemReq.TechnicalDraftId), $"Không tìm thấy cấu hình giá cho vật liệu của bản nháp này.");
                continue;
            }

            // CÔNG THỨC TÍNH GIÁ: (Khối lượng * Giá mỗi gram) * (1 + Phụ thu độ khó)
            decimal unitPrice = (draft.EstimatedWeightPerUnit * currentMaterialPrice.TotalServiceCostPerGram)
                                * (1 + (draft.MarkupPercentage / 100));

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                SourceType = SourceTypes.PrintService, // Loại nguồn hàng là đặt in theo thiết kế
                DesignWorkId = draft.DesignVersionHistory.DesignWorkId,
                TechnicalDraftId = draft.Id,
                ItemName = $"[In theo yêu cầu] {draft.DesignVersionHistory.DesignWork.Name ?? "Bản thiết kế"}",
                QuantityOrdered = itemReq.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * itemReq.Quantity,
                FulfillmentStatus = OrderItemStatuses.Pending,
                Order = order,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username,
                LastModified = CoreHelper.SystemTimeNow,
                LastModifiedBy = _user.Username
            };

            order.OrderItems.Add(orderItem);
            order.TotalPrice += orderItem.TotalPrice;

            draft.IsConfirmed = true;
            draft.LastModified = CoreHelper.SystemTimeNow;
            draft.LastModifiedBy = _user.Username;

            draft.DesignVersionHistory.IsApproved = true;
            draft.DesignVersionHistory.DesignWork.ResultDraftId = draft.Id;
            draft.DesignVersionHistory.DesignWork.Status = DesignWorkStatus.Completed;
            draft.DesignVersionHistory.DesignWork.IsLocked = true;
            draft.DesignVersionHistory.DesignWork.LastModified = CoreHelper.SystemTimeNow;
            draft.DesignVersionHistory.DesignWork.LastModifiedBy = _user.Username;

            _context.DesignLogs.Add(new DesignLog
            {
                Id = Guid.NewGuid(),
                DesignWorkId = draft.DesignVersionHistory.DesignWorkId,
                AccountId = _user.Id != null ? Guid.Parse(_user.Id) : null,
                LogType = DesignLogType.StatusChange,
                Content = "Khách hàng đã xác nhận báo giá kỹ thuật và tạo đơn in. Công việc thiết kế được khóa.",
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "SYSTEM"
            });
        }

        return order;
    }
}
