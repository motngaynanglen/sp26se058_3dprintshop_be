using sp26se058_3dprintshop_be.Domain.Common;

namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;

public static class ShipmentAddressChangeRequestStatuses
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";

    public static readonly List<StatusDefinition> All = new()
    {
        new(Pending, "Chờ xử lý", "Khách hàng đã gửi yêu cầu đổi địa chỉ và đang chờ nhân viên xác minh.", [Approved, Rejected]),
        new(Approved, "Đã duyệt", "Nhân viên đã xác nhận có thể đổi địa chỉ cho lượt giao này.", []),
        new(Rejected, "Đã từ chối", "Yêu cầu đổi địa chỉ không thể thực hiện do hàng đã bàn giao, trạng thái không hợp lệ hoặc lý do vận hành khác.", [])
    };
}
