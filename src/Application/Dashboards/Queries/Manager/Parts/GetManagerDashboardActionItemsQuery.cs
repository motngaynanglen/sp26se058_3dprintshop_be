using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardActionItemsQuery : IRequest<ManagerDashboardActionItemsDTO>;

public class GetManagerDashboardActionItemsQueryHandler : IRequestHandler<GetManagerDashboardActionItemsQuery, ManagerDashboardActionItemsDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardActionItemsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ManagerDashboardActionItemsDTO> Handle(GetManagerDashboardActionItemsQuery request, CancellationToken ct)
    {
        var now = CoreHelper.SystemTimeNow;
        var nowUtcDateTime = now.UtcDateTime;
        var nearExpiryTime = nowUtcDateTime.AddMinutes(5);

        var expiredPendingOrders = await _context.Orders
            .CountAsync(x => x.OrderStatus == OrderStatuses.Pending
                && x.Invoice != null
                && x.Invoice.PaymentStatus == InvoiceStatuses.Unpaid
                && x.Invoice.DueDate != null
                && x.Invoice.DueDate < nowUtcDateTime, ct);

        var nearExpiredPendingOrders = await _context.Orders
            .CountAsync(x => x.OrderStatus == OrderStatuses.Pending
                && x.Invoice != null
                && x.Invoice.PaymentStatus == InvoiceStatuses.Unpaid
                && x.Invoice.DueDate != null
                && x.Invoice.DueDate >= nowUtcDateTime
                && x.Invoice.DueDate <= nearExpiryTime, ct);

        var lowStockCount = await _context.DesignVariants
            .CountAsync(x => x.IsActive && x.StockQuantity <= (x.MinimumStockLevel ?? 5), ct);

        return new ManagerDashboardActionItemsDTO
        {
            GeneratedAt = now,
            ActionItems =
            [
                new() { Key = "expired-pending-orders", Label = "Đơn chờ thanh toán đã quá hạn", Count = expiredPendingOrders, Severity = "DANGER" },
                new() { Key = "near-expired-pending-orders", Label = "Đơn sắp hết hạn thanh toán", Count = nearExpiredPendingOrders, Severity = "WARNING" },
                new() { Key = "pending-address-change-requests", Label = "Yêu cầu đổi địa chỉ chờ xử lý", Count = await _context.ShipmentAddressChangeRequests.CountAsync(x => x.Status == ShipmentAddressChangeRequestStatuses.Pending, ct), Severity = "WARNING" },
                new() { Key = "shipments-preparing", Label = "Vận đơn đang đóng gói", Count = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Preparing, ct), Severity = "INFO" },
                new() { Key = "shipments-ready", Label = "Vận đơn chờ lấy hàng", Count = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.ReadyForPickup, ct), Severity = "INFO" },
                new() { Key = "shipments-failed", Label = "Vận đơn giao thất bại", Count = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Failed, ct), Severity = "WARNING" },
                new() { Key = "shipments-returning", Label = "Vận đơn đang hoàn hàng", Count = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Returning, ct), Severity = "WARNING" },
                new() { Key = "design-works-pending", Label = "Thiết kế chờ tiếp nhận", Count = await _context.DesignWorks.CountAsync(x => x.Status == DesignWorkStatus.Pending, ct), Severity = "INFO" },
                new() { Key = "low-stock-variants", Label = "Biến thể tồn kho thấp", Count = lowStockCount, Severity = "WARNING" }
            ]
        };
    }
}
