namespace sp26se058_3dprintshop_be.Application.StaffDashboard.Models;

public class StaffWorkbenchSlaDto
{
    public int Mf2AssignHours { get; init; } = 4;
    public int ProductionStaleHours { get; init; } = 48;
    public int GhnAfterFinishedHours { get; init; } = 24;
}

public class StaffWorkbenchCountsDto
{
    public int ProductionQueueCount { get; init; }
    public int Mf2Submitted { get; init; }
    public int Mf2Pending { get; init; }
    public int OrdersReadyGhn { get; init; }
}

public class StaffWorkbenchTaskItemDto
{
    public Guid Key { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Meta { get; init; } = string.Empty;
    /// <summary>Deep link FE — mở thẳng đơn/yêu cầu.</summary>
    public string Href { get; init; } = string.Empty;
}

public class StaffWorkbenchTaskDto
{
    public string Id { get; init; } = string.Empty;
    public int Priority { get; init; }
    public string Severity { get; init; } = string.Empty;
    /// <summary>Mã loại việc: QUOTE, ORDER_CONFIRM, PRODUCTION, SHIPPING, FOLLOW_UP.</summary>
    public string TaskType { get; init; } = string.Empty;
    /// <summary>Nhãn loại việc tiếng Việt (Báo giá, Xác nhận đơn, …).</summary>
    public string TaskTypeLabel { get; init; } = string.Empty;
    /// <summary>Mức ưu tiên: HIGH, MEDIUM, LOW.</summary>
    public string PriorityLevel { get; init; } = string.Empty;
    /// <summary>Nhãn ưu tiên tiếng Việt: Cao, Trung bình, Thấp.</summary>
    public string PriorityLabel { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Count { get; init; }
    public string Href { get; init; } = string.Empty;
    /// <summary>Ưu tiên mở mục đầu tiên (nếu có).</summary>
    public string? PrimaryHref { get; init; }
    public string ActionLabel { get; init; } = string.Empty;
    public IReadOnlyList<StaffWorkbenchTaskItemDto> Items { get; init; } = Array.Empty<StaffWorkbenchTaskItemDto>();
}

public class StaffWorkbenchHealthDto
{
    public int Critical { get; init; }
    public int High { get; init; }
    public int Total { get; init; }
    public bool AllClear { get; init; }
}

public class StaffWorkbenchDto
{
    public StaffWorkbenchSlaDto Sla { get; init; } = new();
    public StaffWorkbenchCountsDto Counts { get; init; } = new();
    public StaffWorkbenchHealthDto Health { get; init; } = new();
    public IReadOnlyList<StaffWorkbenchTaskDto> Tasks { get; init; } = Array.Empty<StaffWorkbenchTaskDto>();
}
