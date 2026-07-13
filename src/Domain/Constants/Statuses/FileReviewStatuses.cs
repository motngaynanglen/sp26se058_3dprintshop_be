namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;

/// <summary>
/// Status for DesignVersionHistory.FileReviewStatus.
/// Used by both Design Service (staff marks a design file as printable) and
/// POD/Quick Print (staff reviews customer-submitted STL/OBJ files for technical quality).
/// </summary>
public static class FileReviewStatuses
{
    public const string Pending  = "PENDING";
    public const string Accepted = "ACCEPTED";
    public const string Rejected = "REJECTED";

    public static bool IsValid(string? s) => s is Pending or Accepted or Rejected;

    public static readonly List<StatusDefinition> All = new()
    {
        new(Pending,  "Chờ duyệt",   "File chưa được nhân viên kiểm tra."),
        new(Accepted, "Đã chấp nhận","File đủ tiêu chuẩn kỹ thuật để in."),
        new(Rejected, "Từ chối",     "File không đạt tiêu chuẩn in. Xem FileReviewNote để biết lý do.")
    };
}
