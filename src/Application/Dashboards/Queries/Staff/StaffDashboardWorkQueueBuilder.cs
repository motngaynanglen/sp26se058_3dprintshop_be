using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

internal static class StaffDashboardWorkQueueBuilder
{
    private const int Mf2AssignHours = 4;
    private const int ProductionStaleHours = 48;
    private const int ShippingAfterFinishedHours = 24;

    public static async Task<IReadOnlyCollection<DashboardActionItemDTO>> BuildAsync(
        IApplicationDbContext context,
        IQueryable<Order> assignedOrdersQuery,
        IQueryable<DesignWork> assignedDesignWorksQuery,
        CancellationToken ct)
    {
        var now = CoreHelper.SystemTimeNow;

        var overdueNewDesigns = await context.DesignWorks.CountAsync(d =>
            (d.Status == DesignWorkStatus.Sketching || d.Status == DesignWorkStatus.Pending)
            && d.Created <= now.AddHours(-Mf2AssignHours), ct);

        var staleProduction = await assignedOrdersQuery
            .Where(o => o.OrderStatus == OrderStatuses.Processing
                && o.Invoice != null
                && o.Invoice.PaymentStatus == InvoiceStatuses.Paid
                && (o.DepositedAt ?? o.Created) <= now.AddHours(-ProductionStaleHours)
                && o.OrderItems.Any(oi =>
                    (oi.SourceType == SourceTypes.DesignService
                        || oi.SourceType == SourceTypes.PrintService
                        || oi.SourceType == SourceTypes.PreOrder)
                    && oi.FulfillmentStatus != OrderItemStatuses.Finished
                    && oi.FulfillmentStatus != OrderItemStatuses.Cancelled))
            .CountAsync(ct);

        var shipmentOverdue = await assignedOrdersQuery
            .Where(o => o.OrderStatus == OrderStatuses.Finished
                && o.Invoice != null
                && o.Invoice.PaymentStatus == InvoiceStatuses.Paid
                && !o.Shipments.Any(s => s.CarrierOrderCode != null && s.CarrierOrderCode != "")
                && o.LastModified <= now.AddHours(-ShippingAfterFinishedHours))
            .CountAsync(ct);

        var assignedProcessingOrders = await assignedOrdersQuery.CountAsync(x => x.OrderStatus == OrderStatuses.Processing, ct);
        var assignedFinishedOrders = await assignedOrdersQuery.CountAsync(x => x.OrderStatus == OrderStatuses.Finished, ct);
        var assignedDesignInProgress = await assignedDesignWorksQuery.CountAsync(x => x.Status == DesignWorkStatus.InProgress, ct);
        var assignedDesignReviewing = await assignedDesignWorksQuery.CountAsync(x => x.Status == DesignWorkStatus.Reviewing, ct);
        var pendingAddressChangeRequests = await context.ShipmentAddressChangeRequests
            .CountAsync(x => x.Status == ShipmentAddressChangeRequestStatuses.Pending, ct);
        var shipmentsPreparing = await context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Preparing, ct);
        var shipmentsReady = await context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.ReadyForPickup, ct);
        var shipmentsFailed = await context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Failed, ct);
        var shipmentsReturning = await context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Returning, ct);

        return
        [
            new() { Key = "mf2-overdue", Label = "Thiết kế mới quá hạn tiếp nhận (>=4h)", Count = overdueNewDesigns, Severity = "DANGER", Href = "/staff/custom-orders" },
            new() { Key = "production-stale", Label = "Sản xuất chậm (>48h chưa in xong)", Count = staleProduction, Severity = "DANGER", Href = "/staff/production-queue" },
            new() { Key = "ghn-overdue", Label = "Chậm tạo vận đơn (>24h sau khi sẵn sàng giao)", Count = shipmentOverdue, Severity = "WARNING", Href = "/staff/shop-orders" },
            new() { Key = "assigned-processing-orders", Label = "Đơn đang xử lý", Count = assignedProcessingOrders, Severity = "INFO", Href = "/staff/shop-orders" },
            new() { Key = "assigned-finished-orders", Label = "Đơn chờ giao/hoàn tất", Count = assignedFinishedOrders, Severity = "INFO", Href = "/staff/shop-orders" },
            new() { Key = "assigned-design-in-progress", Label = "Thiết kế đang thực hiện", Count = assignedDesignInProgress, Severity = "INFO", Href = "/staff/custom-orders" },
            new() { Key = "assigned-design-reviewing", Label = "Thiết kế chờ duyệt", Count = assignedDesignReviewing, Severity = "WARNING", Href = "/staff/custom-orders" },
            new() { Key = "pending-address-change-requests", Label = "Yêu cầu đổi địa chỉ chờ xử lý", Count = pendingAddressChangeRequests, Severity = "WARNING", Href = "/staff/shop-orders" },
            new() { Key = "shipments-preparing", Label = "Vận đơn đang đóng gói", Count = shipmentsPreparing, Severity = "INFO", Href = "/staff/shop-orders" },
            new() { Key = "shipments-ready", Label = "Vận đơn chờ lấy hàng", Count = shipmentsReady, Severity = "INFO", Href = "/staff/shop-orders" },
            new() { Key = "shipments-failed", Label = "Vận đơn giao thất bại", Count = shipmentsFailed, Severity = "WARNING", Href = "/staff/shop-orders" },
            new() { Key = "shipments-returning", Label = "Vận đơn đang hoàn hàng", Count = shipmentsReturning, Severity = "WARNING", Href = "/staff/shop-orders" }
        ];
    }
}
