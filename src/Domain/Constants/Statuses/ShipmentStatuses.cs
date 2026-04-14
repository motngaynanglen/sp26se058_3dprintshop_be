using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;
public static class ShipmentStatuses
{
    // Trạng thái Giao hàng (Logistics)

    public const string Preparing = "PREPARING";
    public const string ReadyForPickup = "READY_FOR_PICKUP";
    public const string InTransit = "IN_TRANSIT";
    public const string Delivered = "DELIVERED";
    public const string Cancelled = "CANCELLED";
    // Các trạng thái cân nhắc thêm vào:
    public const string Failed = "FAILED";
    public const string Returned = "RETURNED";


    public static readonly List<StatusDefinition> All = new()
    {
        new(Preparing, "Đang đóng gói", "Đang kiểm tra chất lượng và đóng hộp."),
        new(ReadyForPickup, "Chờ lấy hàng", "Đã sẵn sàng, chờ đơn vị vận chuyển hoặc khách đến lấy."),
        new(InTransit, "Đang giao", "Đơn hàng đang trong quá trình vận chuyển."),
        new(Delivered, "Giao thành công", "Khách hàng đã nhận hàng và ký xác nhận."),
        new(Cancelled, "Đã hủy", "Yêu cầu giao hàng đã bị hủy bỏ."),
        new(Failed, "Giao thất bại", "Không liên lạc được khách hoặc địa chỉ sai."),
        new(Returned, "Đã hoàn hàng", "Hàng đã quay trở lại kho.")

    };
}
