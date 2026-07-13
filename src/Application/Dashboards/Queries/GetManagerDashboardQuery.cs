using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;
using System.Linq.Expressions;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardQuery : IRequest<ManagerDashboardDTO>;

public class GetManagerDashboardQueryHandler : IRequestHandler<GetManagerDashboardQuery, ManagerDashboardDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ManagerDashboardDTO> Handle(GetManagerDashboardQuery request, CancellationToken ct)
    {
        var now = CoreHelper.SystemTimeNow;
        var nowUtcDateTime = now.UtcDateTime;
        var currentMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var nearExpiryTime = nowUtcDateTime.AddMinutes(5);

        var orderCounts = await CountByStatusAsync(_context.Orders, x => x.OrderStatus, ct);
        var orderItemCounts = await CountByStatusAsync(_context.OrderItems, x => x.FulfillmentStatus, ct);
        var shipmentCounts = await CountByStatusAsync(_context.Shipments, x => x.ShipmentStatus, ct);
        var designWorkCounts = await CountByStatusAsync(_context.DesignWorks, x => x.Status, ct);
        var invoiceCounts = await CountByStatusAsync(_context.Invoices, x => x.PaymentStatus, ct);

        var paidRevenue = await _context.Invoices
            .Where(x => x.PaymentStatus == InvoiceStatuses.Paid)
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;

        var currentMonthPaidRevenue = await _context.Invoices
            .Where(x => x.PaymentStatus == InvoiceStatuses.Paid && x.Created >= currentMonthStart)
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;

        var unpaidAmount = await _context.Invoices
            .Where(x => x.PaymentStatus == InvoiceStatuses.Unpaid)
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;

        var paidInvoiceCount = await _context.Invoices.CountAsync(x => x.PaymentStatus == InvoiceStatuses.Paid, ct);
        var unpaidInvoiceCount = await _context.Invoices.CountAsync(x => x.PaymentStatus == InvoiceStatuses.Unpaid, ct);

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

        var pendingAddressChangeRequests = await _context.ShipmentAddressChangeRequests
            .CountAsync(x => x.Status == ShipmentAddressChangeRequestStatuses.Pending, ct);

        var shipmentsPreparing = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Preparing, ct);
        var shipmentsReady = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.ReadyForPickup, ct);
        var shipmentsFailed = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Failed, ct);
        var shipmentsReturning = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Returning, ct);
        var designWorksPending = await _context.DesignWorks.CountAsync(x => x.Status == DesignWorkStatus.Pending, ct);

        var lowStockVariants = await _context.DesignVariants
            .Where(x => x.IsActive && x.StockQuantity <= (x.MinimumStockLevel ?? 5))
            .OrderBy(x => x.StockQuantity)
            .Take(10)
            .Select(x => new DashboardLowStockVariantDTO
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                StockQuantity = x.StockQuantity,
                MinimumStockLevel = x.MinimumStockLevel ?? 5
            })
            .ToListAsync(ct);

        var recentOrders = await _context.Orders
            .Include(x => x.Customer)
                .ThenInclude(x => x.Account)
            .OrderByDescending(x => x.Created)
            .Take(10)
            .Select(x => new DashboardRecentOrderDTO
            {
                Id = x.Id,
                Code = x.Code,
                CustomerName = x.Customer.Account.Fullname,
                TotalPrice = x.TotalPrice,
                OrderStatus = x.OrderStatus,
                Created = x.Created
            })
            .ToListAsync(ct);

        var actionItems = new List<DashboardActionItemDTO>
        {
            new() { Key = "expired-pending-orders", Label = "Đơn chờ thanh toán đã quá hạn", Count = expiredPendingOrders, Severity = "DANGER" },
            new() { Key = "near-expired-pending-orders", Label = "Đơn sắp hết hạn thanh toán", Count = nearExpiredPendingOrders, Severity = "WARNING" },
            new() { Key = "pending-address-change-requests", Label = "Yêu cầu đổi địa chỉ chờ xử lý", Count = pendingAddressChangeRequests, Severity = "WARNING" },
            new() { Key = "shipments-preparing", Label = "Vận đơn đang đóng gói", Count = shipmentsPreparing, Severity = "INFO" },
            new() { Key = "shipments-ready", Label = "Vận đơn chờ lấy hàng", Count = shipmentsReady, Severity = "INFO" },
            new() { Key = "shipments-failed", Label = "Vận đơn giao thất bại", Count = shipmentsFailed, Severity = "WARNING" },
            new() { Key = "shipments-returning", Label = "Vận đơn đang hoàn hàng", Count = shipmentsReturning, Severity = "WARNING" },
            new() { Key = "design-works-pending", Label = "Thiết kế chờ tiếp nhận", Count = designWorksPending, Severity = "INFO" },
            new() { Key = "low-stock-variants", Label = "Biến thể tồn kho thấp", Count = lowStockVariants.Count, Severity = "WARNING" }
        };

        return new ManagerDashboardDTO
        {
            GeneratedAt = now,
            OrdersByStatus = DashboardStatusHelper.Merge(OrderStatuses.All, orderCounts),
            OrderItemsByStatus = DashboardStatusHelper.Merge(OrderItemStatuses.All, orderItemCounts),
            ShipmentsByStatus = DashboardStatusHelper.Merge(ShipmentStatuses.All, shipmentCounts),
            DesignWorksByStatus = DashboardStatusHelper.Merge(DesignWorkStatus.All, designWorkCounts),
            InvoicesByStatus = DashboardStatusHelper.Merge(InvoiceStatuses.All, invoiceCounts),
            Revenue = new DashboardRevenueDTO
            {
                PaidRevenue = paidRevenue,
                CurrentMonthPaidRevenue = currentMonthPaidRevenue,
                UnpaidAmount = unpaidAmount,
                PaidInvoiceCount = paidInvoiceCount,
                UnpaidInvoiceCount = unpaidInvoiceCount
            },
            ActionItems = actionItems,
            LowStockVariants = lowStockVariants,
            RecentOrders = recentOrders
        };
    }

    private static async Task<IReadOnlyCollection<DashboardStatusCountDTO>> CountByStatusAsync<TEntity>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, string>> statusSelector,
        CancellationToken ct)
    {
        return await query
            .GroupBy(statusSelector)
            .Select(x => new DashboardStatusCountDTO
            {
                Status = x.Key,
                Label = string.Empty,
                Count = x.Count()
            })
            .ToListAsync(ct);
    }
}
