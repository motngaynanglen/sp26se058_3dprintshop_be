namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

public class DashboardStatusCountDTO
{
    public string Status { get; init; } = null!;
    public string Label { get; init; } = null!;
    public int Count { get; init; }
}

public class DashboardActionItemDTO
{
    public string Key { get; init; } = null!;
    public string Label { get; init; } = null!;
    public int Count { get; init; }
    public string Severity { get; init; } = "INFO";
}

public class DashboardRevenueDTO
{
    public decimal PaidRevenue { get; init; }
    public decimal CurrentMonthPaidRevenue { get; init; }
    public decimal UnpaidAmount { get; init; }
    public int PaidInvoiceCount { get; init; }
    public int UnpaidInvoiceCount { get; init; }
}

public class DashboardRecentOrderDTO
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string? CustomerName { get; init; }
    public decimal TotalPrice { get; init; }
    public string OrderStatus { get; init; } = null!;
    public DateTimeOffset Created { get; init; }
}

public class DashboardDesignWorkDTO
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string Status { get; init; } = null!;
    public string? CustomerName { get; init; }
    public DateTimeOffset Created { get; init; }
}

public class DashboardLowStockVariantDTO
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public int StockQuantity { get; init; }
    public int MinimumStockLevel { get; init; }
}

public class ManagerDashboardDTO
{
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyCollection<DashboardStatusCountDTO> OrdersByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> OrderItemsByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> ShipmentsByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> DesignWorksByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> InvoicesByStatus { get; init; } = [];
    public DashboardRevenueDTO Revenue { get; init; } = new();
    public IReadOnlyCollection<DashboardActionItemDTO> ActionItems { get; init; } = [];
    public IReadOnlyCollection<DashboardLowStockVariantDTO> LowStockVariants { get; init; } = [];
    public IReadOnlyCollection<DashboardRecentOrderDTO> RecentOrders { get; init; } = [];
}

public class ManagerDashboardStatusSummaryDTO
{
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyCollection<DashboardStatusCountDTO> OrdersByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> OrderItemsByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> ShipmentsByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> DesignWorksByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> InvoicesByStatus { get; init; } = [];
}

public class ManagerDashboardRevenueSummaryDTO
{
    public DateTimeOffset GeneratedAt { get; init; }
    public DashboardRevenueDTO Revenue { get; init; } = new();
}

public class ManagerDashboardActionItemsDTO
{
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyCollection<DashboardActionItemDTO> ActionItems { get; init; } = [];
}

public class ManagerDashboardInventorySummaryDTO
{
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyCollection<DashboardLowStockVariantDTO> LowStockVariants { get; init; } = [];
}

public class ManagerDashboardRecentOrdersDTO
{
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyCollection<DashboardRecentOrderDTO> RecentOrders { get; init; } = [];
}

public class StaffDashboardDTO
{
    public DateTimeOffset GeneratedAt { get; init; }
    public Guid? StaffId { get; init; }
    public string? StaffName { get; init; }
    public IReadOnlyCollection<DashboardStatusCountDTO> AssignedOrdersByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> AssignedDesignWorksByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> ShipmentsByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardActionItemDTO> WorkQueue { get; init; } = [];
    public IReadOnlyCollection<DashboardRecentOrderDTO> RecentAssignedOrders { get; init; } = [];
    public IReadOnlyCollection<DashboardDesignWorkDTO> RecentAssignedDesignWorks { get; init; } = [];
}

public class StaffDashboardSummaryDTO
{
    public DateTimeOffset GeneratedAt { get; init; }
    public Guid? StaffId { get; init; }
    public string? StaffName { get; init; }
    public IReadOnlyCollection<DashboardStatusCountDTO> AssignedOrdersByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> AssignedDesignWorksByStatus { get; init; } = [];
    public IReadOnlyCollection<DashboardStatusCountDTO> ShipmentsByStatus { get; init; } = [];
}

public class StaffDashboardWorkQueueDTO
{
    public DateTimeOffset GeneratedAt { get; init; }
    public Guid? StaffId { get; init; }
    public string? StaffName { get; init; }
    public IReadOnlyCollection<DashboardActionItemDTO> WorkQueue { get; init; } = [];
}

public class StaffDashboardRecentWorkDTO
{
    public DateTimeOffset GeneratedAt { get; init; }
    public Guid? StaffId { get; init; }
    public string? StaffName { get; init; }
    public IReadOnlyCollection<DashboardRecentOrderDTO> RecentAssignedOrders { get; init; } = [];
    public IReadOnlyCollection<DashboardDesignWorkDTO> RecentAssignedDesignWorks { get; init; } = [];
}
