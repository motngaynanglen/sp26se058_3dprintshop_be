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
    /* Các trạng thái cân nhắc thêm vào:
    public const string Failed = "FAILED";
    public const string Returned = "RETURNED";
    */

    public static readonly List<StatusDefinition> All = new()
    {
        new(Preparing, "Đang đóng gói", "#FFC107", "Đang kiểm tra chất lượng và đóng hộp."),
        new(ReadyForPickup, "Chờ lấy hàng", "#FF9800", "Đã sẵn sàng, chờ đơn vị vận chuyển hoặc khách đến lấy."),
        new(InTransit, "Đang giao", "#03A9F4", "Đơn hàng đang trong quá trình vận chuyển."),
        new(Delivered, "Giao thành công", "#4CAF50", "Khách hàng đã nhận hàng và ký xác nhận."),
        /*
        new(Failed, "Giao thất bại", "#F44336", "Không liên lạc được khách hoặc địa chỉ sai."),
        new(Returned, "Đã hoàn hàng", "#E91E63", "Hàng đã quay trở lại kho.")
        */
    };
}
