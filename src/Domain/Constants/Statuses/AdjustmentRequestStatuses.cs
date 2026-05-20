namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;

public static class AdjustmentRequestStatuses
{
    public const string PendingReview = "PENDING_REVIEW";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";

    public static readonly List<StatusDefinition> All = new()
    {
        new(PendingReview, "Chờ duyệt", "Yêu cầu hiệu chỉnh đã được tổng hợp từ trao đổi/chat và đang chờ nhân viên xác nhận phạm vi."),
        new(Approved, "Đã duyệt", "Yêu cầu nằm trong phạm vi brief ban đầu và đã tiêu một lượt hiệu chỉnh."),
        new(Rejected, "Từ chối", "Yêu cầu vượt phạm vi hoặc không phù hợp để xử lý trong công việc thiết kế hiện tại.")
    };
}
