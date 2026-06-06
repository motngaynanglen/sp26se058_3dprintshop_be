using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;

public record UpdateOrderItemFulfillmentResult(
    Guid OrderItemId,
    string FulfillmentStatus,
    Guid OrderId,
    string OrderCode,
    bool AllProductionLinesFinished,
    string? Message);

/// <summary>[Staff/Manager] Cập nhật tiến độ sản xuất từng dòng hàng (in 3D, hoàn thiện).</summary>
public record UpdateOrderItemFulfillmentCommand : IRequest<UpdateOrderItemFulfillmentResult>
{
    [JsonIgnore]
    public Guid OrderItemId { get; init; }

    public required string FulfillmentStatus { get; init; }

    public string? Note { get; init; }
}

public class UpdateOrderItemFulfillmentCommandValidator : AbstractValidator<UpdateOrderItemFulfillmentCommand>
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        OrderItemStatuses.Pending,
        OrderItemStatuses.Designing,
        OrderItemStatuses.Printing,
        OrderItemStatuses.Finished
    };

    public UpdateOrderItemFulfillmentCommandValidator()
    {
        RuleFor(x => x.OrderItemId).NotEmpty();
        RuleFor(x => x.FulfillmentStatus)
            .NotEmpty()
            .Must(s => Allowed.Contains(s))
            .WithMessage("FulfillmentStatus không hợp lệ.");
    }
}

public class UpdateOrderItemFulfillmentCommandHandler
    : IRequestHandler<UpdateOrderItemFulfillmentCommand, UpdateOrderItemFulfillmentResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateOrderItemFulfillmentCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<UpdateOrderItemFulfillmentResult> Handle(
        UpdateOrderItemFulfillmentCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var role = _user.Role ?? Roles.GUEST;
        if (role != Roles.STAFF && role != Roles.MANAGER && role != Roles.ADMIN)
            throw new UnauthorizedAccessException("Chỉ nhân viên/quản lý cập nhật tiến độ sản xuất.");

        var item = await _context.OrderItems
            .Include(oi => oi.Order).ThenInclude(o => o.Invoice!).ThenInclude(i => i.Transactions)
            .FirstOrDefaultAsync(oi => oi.Id == request.OrderItemId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy dòng hàng.");

        var order = item.Order;
        if (order.OrderStatus == OrderStatuses.Cancelled)
            throw new InvalidOperationException("Đơn hàng đã hủy.");

        if (order.OrderStatus is OrderStatuses.Completed)
            throw new InvalidOperationException("Đơn đã hoàn thành, không cập nhật sản xuất.");

        if (!OrderPaymentHelper.CanProceedToFulfillment(order.Invoice))
            throw new InvalidOperationException("Đơn chưa thanh toán — chưa vào hàng đợi sản xuất.");

        if (order.OrderStatus != OrderStatuses.Processing)
            throw new InvalidOperationException(
                "Chỉ cập nhật sản xuất khi đơn đang PROCESSING (đang làm hàng).");

        var status = request.FulfillmentStatus.Trim().ToUpperInvariant();
        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? "staff";

        item.FulfillmentStatus = status;
        item.LastModified = now;
        item.LastModifiedBy = username;
        order.LastModified = now;
        order.LastModifiedBy = username;

        await _context.SaveChangesAsync(cancellationToken);

        var activeItems = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.OrderId == order.Id
                         && oi.FulfillmentStatus != OrderItemStatuses.Cancelled)
            .ToListAsync(cancellationToken);

        var allFinished = activeItems.Count > 0
                          && activeItems.All(oi => oi.FulfillmentStatus == OrderItemStatuses.Finished);

        string? msg = null;
        if (allFinished)
            msg = "Tất cả dòng hàng đã hoàn thiện — chuyển đơn sang «Sẵn sàng giao» rồi tạo vận đơn GHN.";

        return new UpdateOrderItemFulfillmentResult(
            item.Id,
            status,
            order.Id,
            order.Code,
            allFinished,
            msg);
    }
}
