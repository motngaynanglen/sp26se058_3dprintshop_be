namespace sp26se058_3dprintshop_be.Application.StaffDashboard;

/// <summary>Loại việc trên bàn làm việc KTV.</summary>
public static class StaffWorkbenchTaskTypes
{
    public const string Quote = "QUOTE";
    public const string OrderConfirm = "ORDER_CONFIRM";
    public const string Production = "PRODUCTION";
    public const string Shipping = "SHIPPING";
    public const string FollowUp = "FOLLOW_UP";

    public static string Label(string taskType) => taskType switch
    {
        Quote => "Báo giá",
        OrderConfirm => "Xác nhận đơn",
        Production => "Sản xuất",
        Shipping => "Giao hàng",
        FollowUp => "Theo dõi",
        _ => taskType
    };
}

/// <summary>Độ ưu tiên hiển thị (Thấp / Trung bình / Cao).</summary>
public static class StaffWorkbenchPriorityLevels
{
    public const string High = "HIGH";
    public const string Medium = "MEDIUM";
    public const string Low = "LOW";

    public static string Label(string level) => level switch
    {
        High => "Cao",
        Medium => "Trung bình",
        Low => "Thấp",
        _ => level
    };

    public static string FromSeverity(string severity) => severity switch
    {
        "critical" or "high" => High,
        "medium" => Medium,
        _ => Low
    };

    public static int SortOrder(string level) => level switch
    {
        High => 0,
        Medium => 1,
        _ => 2
    };
}
