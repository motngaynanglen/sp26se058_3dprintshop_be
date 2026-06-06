namespace sp26se058_3dprintshop_be.Application.ManagerDashboard.Models;

public class ManagerDashboardDto
{
    public RevenueReportDto Revenue { get; set; } = new();
    public List<MaterialStockReportItemDto> MaterialStock { get; set; } = [];
    public List<DashboardFeedbackItemDto> RecentFeedbacks { get; set; } = [];
}

public class RevenueReportDto
{
    public decimal TotalCollected { get; set; }
    public decimal ThisMonthCollected { get; set; }
    public int SuccessfulTransactionCount { get; set; }
    public decimal PendingInvoiceAmount { get; set; }
    public int PendingInvoiceCount { get; set; }
    public List<MonthlyRevenueItemDto> MonthlyTrend { get; set; } = [];
}

public class MonthlyRevenueItemDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class MaterialStockReportItemDto
{
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public decimal StockQuantityGrams { get; set; }
    public bool IsLowStock { get; set; }
    public int TotalStock { get; set; }
    public int VariantCount { get; set; }
    public int ActiveVariantCount { get; set; }
    public int LowStockVariantCount { get; set; }
}

public class DashboardFeedbackItemDto
{
    public Guid Id { get; set; }
    public string CustomerFullName { get; set; } = string.Empty;
    public string? DesignTemplateName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? StaffReply { get; set; }
    public bool IsHidden { get; set; }
    public DateTimeOffset Created { get; set; }
}
