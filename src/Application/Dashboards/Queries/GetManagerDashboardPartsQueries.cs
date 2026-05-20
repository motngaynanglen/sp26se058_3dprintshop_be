using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardStatusSummaryQuery : IRequest<ManagerDashboardStatusSummaryDTO>;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardRevenueQuery : IRequest<ManagerDashboardRevenueSummaryDTO>;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardActionItemsQuery : IRequest<ManagerDashboardActionItemsDTO>;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardInventoryQuery : IRequest<ManagerDashboardInventorySummaryDTO>;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardRecentOrdersQuery : IRequest<ManagerDashboardRecentOrdersDTO>;

public class GetManagerDashboardStatusSummaryQueryHandler : IRequestHandler<GetManagerDashboardStatusSummaryQuery, ManagerDashboardStatusSummaryDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardStatusSummaryQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ManagerDashboardStatusSummaryDTO> Handle(GetManagerDashboardStatusSummaryQuery request, CancellationToken ct)
    {
        var orderCounts = await DashboardStatusHelper.CountByStatusAsync(_context.Orders, x => x.OrderStatus, ct);
        var orderItemCounts = await DashboardStatusHelper.CountByStatusAsync(_context.OrderItems, x => x.FulfillmentStatus, ct);
        var shipmentCounts = await DashboardStatusHelper.CountByStatusAsync(_context.Shipments, x => x.ShipmentStatus, ct);
        var designWorkCounts = await DashboardStatusHelper.CountByStatusAsync(_context.DesignWorks, x => x.Status, ct);
        var invoiceCounts = await DashboardStatusHelper.CountByStatusAsync(_context.Invoices, x => x.PaymentStatus, ct);

        return new ManagerDashboardStatusSummaryDTO
        {
            GeneratedAt = CoreHelper.SystemTimeNow,
            OrdersByStatus = DashboardStatusHelper.Merge(OrderStatuses.All, orderCounts),
            OrderItemsByStatus = DashboardStatusHelper.Merge(OrderItemStatuses.All, orderItemCounts),
            ShipmentsByStatus = DashboardStatusHelper.Merge(ShipmentStatuses.All, shipmentCounts),
            DesignWorksByStatus = DashboardStatusHelper.Merge(DesignWorkStatus.All, designWorkCounts),
            InvoicesByStatus = DashboardStatusHelper.Merge(InvoiceStatuses.All, invoiceCounts)
        };
    }
}

public class GetManagerDashboardRevenueQueryHandler : IRequestHandler<GetManagerDashboardRevenueQuery, ManagerDashboardRevenueSummaryDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardRevenueQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ManagerDashboardRevenueSummaryDTO> Handle(GetManagerDashboardRevenueQuery request, CancellationToken ct)
    {
        var now = CoreHelper.SystemTimeNow;
        var currentMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var paidRevenue = await _context.Invoices
            .Where(x => x.PaymentStatus == InvoiceStatuses.Paid)
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;

        var currentMonthPaidRevenue = await _context.Invoices
            .Where(x => x.PaymentStatus == InvoiceStatuses.Paid && x.Created >= currentMonthStart)
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;

        var unpaidAmount = await _context.Invoices
            .Where(x => x.PaymentStatus == InvoiceStatuses.Unpaid)
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;

        return new ManagerDashboardRevenueSummaryDTO
        {
            GeneratedAt = now,
            Revenue = new DashboardRevenueDTO
            {
                PaidRevenue = paidRevenue,
                CurrentMonthPaidRevenue = currentMonthPaidRevenue,
                UnpaidAmount = unpaidAmount,
                PaidInvoiceCount = await _context.Invoices.CountAsync(x => x.PaymentStatus == InvoiceStatuses.Paid, ct),
                UnpaidInvoiceCount = await _context.Invoices.CountAsync(x => x.PaymentStatus == InvoiceStatuses.Unpaid, ct)
            }
        };
    }
}

public class GetManagerDashboardActionItemsQueryHandler : IRequestHandler<GetManagerDashboardActionItemsQuery, ManagerDashboardActionItemsDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardActionItemsQueryHandler(IApplicationDbContext context) => _context = context;

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

public class GetManagerDashboardInventoryQueryHandler : IRequestHandler<GetManagerDashboardInventoryQuery, ManagerDashboardInventorySummaryDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardInventoryQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ManagerDashboardInventorySummaryDTO> Handle(GetManagerDashboardInventoryQuery request, CancellationToken ct)
    {
        return new ManagerDashboardInventorySummaryDTO
        {
            GeneratedAt = CoreHelper.SystemTimeNow,
            LowStockVariants = await _context.DesignVariants
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
                .ToListAsync(ct)
        };
    }
}

public class GetManagerDashboardRecentOrdersQueryHandler : IRequestHandler<GetManagerDashboardRecentOrdersQuery, ManagerDashboardRecentOrdersDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardRecentOrdersQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ManagerDashboardRecentOrdersDTO> Handle(GetManagerDashboardRecentOrdersQuery request, CancellationToken ct)
    {
        return new ManagerDashboardRecentOrdersDTO
        {
            GeneratedAt = CoreHelper.SystemTimeNow,
            RecentOrders = await _context.Orders
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
                .ToListAsync(ct)
        };
    }
}
