using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;
public static class OrderItemStatuses
{
    // Trạng thái Sản phẩm/Hạng mục trong đơn (Tiến độ xưởng)
    public const string Pending = "PENDING";
    public const string Designing = "DESIGNING";
    public const string Printing = "PRINTING";
    public const string Picking = "PICKING";
    public const string Finished = "FINISHED";
    public const string Cancelled = "CANCELLED";

    public static readonly List<StatusDefinition> All = new()
    {
        new(Pending, "Đang chờ", "Mục hàng đang xếp hàng chờ xử lý."),
        new(Designing, "Đang thiết kế", "Đang trong giai đoạn vẽ mẫu hoặc chỉnh sửa tệp 3D."),
        new(Printing, "Đang in 3D", "Mẫu đang được chạy thực tế trên máy in 3D."),
        new(Picking, "Đang soạn hàng", "Đang lấy sản phẩm có sẵn từ kho hoặc chuẩn bị linh kiện."),
        new(Finished, "Hoàn thiện", "Đã xong, đang chờ gộp đơn để giao hàng."),
        new(Cancelled, "Đã hủy", "Mục hàng đã bị dừng xử lý.")
    };
}
