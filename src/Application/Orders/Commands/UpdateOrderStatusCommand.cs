using System.ComponentModel;
using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;

/// <summary>
/// [Staff/Manager] Cập nhật tiến độ đơn hàng — đóng gói, đang giao, đã giao, hoàn thành.
/// Áp dụng cho cả Flow 1 (template hàng có sẵn) và Flow 2/3 (in theo yêu cầu).
/// </summary>
public record UpdateOrderStatusCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid OrderId { get; init; }

    [DefaultValue(OrderStatuses.Processing)]
    public string OrderStatus { get; init; } = string.Empty;

    /// <summary>(tùy chọn) cập nhật shipment song song. Giá trị từ <see cref="ShipmentStatuses"/>.</summary>
    public string? ShipmentStatus { get; init; }

    /// <summary>(tùy chọn) số tracking của đơn vị vận chuyển.</summary>
    public string? TrackingNumber { get; init; }

    public string? Note { get; init; }
}

public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    private static readonly HashSet<string> AllowedOrderStatuses = new()
    {
        OrderStatuses.Processing,
        OrderStatuses.Finished,
        OrderStatuses.Completed
    };

    private static readonly HashSet<string> AllowedShipmentStatuses = new()
    {
        ShipmentStatuses.Preparing,
        ShipmentStatuses.ReadyForPickup,
        ShipmentStatuses.InTransit,
        ShipmentStatuses.Delivered
    };

    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.OrderStatus)
            .NotEmpty()
            .Must(s => AllowedOrderStatuses.Contains(s))
            .WithMessage("OrderStatus phải thuộc: PROCESSING / FINISHED / COMPLETED.");
        When(x => !string.IsNullOrWhiteSpace(x.ShipmentStatus), () =>
        {
            RuleFor(x => x.ShipmentStatus!)
                .Must(s => AllowedShipmentStatuses.Contains(s))
                .WithMessage("ShipmentStatus không hợp lệ.");
        });
    }
}

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateOrderStatusCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var role = _user.Role ?? Roles.GUEST;
        if (role != Roles.STAFF && role != Roles.MANAGER && role != Roles.ADMIN)
            throw new UnauthorizedAccessException("Chỉ nhân viên/quản lý cập nhật được trạng thái đơn.");

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Invoice!).ThenInclude(i => i.Transactions)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? "staff";

        order.OrderStatus = request.OrderStatus;
        order.LastModified = now;
        order.LastModifiedBy = username;

        if (request.OrderStatus == OrderStatuses.Completed && order.CompletedAt == null)
            order.CompletedAt = now;

        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.OrderId == order.Id, cancellationToken);

        if (shipment != null)
        {
            if (!string.IsNullOrWhiteSpace(request.ShipmentStatus))
            {
                shipment.ShipmentStatus = request.ShipmentStatus!;
                shipment.LastModified = now;
                shipment.LastModifiedBy = username;

                if (request.ShipmentStatus == ShipmentStatuses.InTransit && shipment.ShippedAt == null)
                    shipment.ShippedAt = now.UtcDateTime;

                if (request.ShipmentStatus == ShipmentStatuses.Delivered)
                {
                    shipment.DeliveredAt = now.UtcDateTime;
                    order.DeliveredAt = now;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.TrackingNumber))
            {
                shipment.TrackingNumber = request.TrackingNumber!.Trim();
                shipment.LastModified = now;
                shipment.LastModifiedBy = username;
            }
        }

        if (request.OrderStatus == OrderStatuses.Completed)
        {
            foreach (var item in order.OrderItems.Where(i => i.FulfillmentStatus != OrderItemStatuses.Cancelled))
            {
                item.FulfillmentStatus = OrderItemStatuses.Finished;
                item.LastModified = now;
                item.LastModifiedBy = username;
            }

            if (order.Invoice != null && OrderPaymentHelper.IsCodOrder(order.Invoice))
            {
                order.Invoice.PaymentStatus = InvoiceStatuses.Paid;
                order.Invoice.LastModified = now;
                order.Invoice.LastModifiedBy = username;

                foreach (var tx in order.Invoice.Transactions.Where(OrderPaymentHelper.IsActiveCodTransaction))
                {
                    tx.TransactionStatus = "SUCCESS";
                    tx.Note = (tx.Note ?? "") + " [COD] Thu tiền khi giao hàng thành công.";
                    tx.LastModified = now;
                    tx.LastModifiedBy = username;
                }

                if (order.DepositedAt == null)
                    order.DepositedAt = now;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
