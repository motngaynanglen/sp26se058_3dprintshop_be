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
    public const string Failed = "FAILED";
    public const string Returning = "RETURNING";
    public const string Returned = "RETURNED";
    public const string LostOrDamaged = "LOST_OR_DAMAGED";


    public static readonly List<StatusDefinition> All = new()
    {
        new(Preparing, "Đang đóng gói", "Đang kiểm tra chất lượng và đóng hộp.", [ReadyForPickup, Cancelled]),
        new(ReadyForPickup, "Chờ lấy hàng", "Đã sẵn sàng, chờ đơn vị vận chuyển hoặc khách đến lấy.", [InTransit, Cancelled]),
        new(InTransit, "Đang giao", "Đơn hàng đang trong quá trình vận chuyển.", [Delivered, Failed, Returning]),
        new(Delivered, "Giao thành công", "Khách hàng đã nhận hàng và ký xác nhận.", []),
        new(Cancelled, "Đã hủy", "Lượt giao hàng đã bị hủy bỏ. Trạng thái này không đồng nghĩa với hủy đơn hàng.", []),
        new(Failed, "Giao thất bại", "Không liên lạc được khách, khách hẹn lại, từ chối nhận hoặc địa chỉ sai.", [Returning]),
        new(Returning, "Đang hoàn hàng", "Kiện hàng đang được chuyển hoàn về shop hoặc kho.", [Returned, LostOrDamaged]),
        new(Returned, "Đã hoàn hàng", "Hàng đã quay trở lại shop hoặc kho.", []),
        new(LostOrDamaged, "Thất lạc hoặc hư hỏng", "Kiện hàng bị thất lạc hoặc hư hỏng trong quá trình vận chuyển/hoàn hàng.", [])

    };
}
