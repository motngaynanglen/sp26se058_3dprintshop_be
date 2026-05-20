using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;
public static class OrderStatuses
{
    // 1. Trạng thái Đơn hàng (Tổng quát)
    public const string Pending = "PENDING";
    //public const string Deposited = "DEPOSITED";
    public const string Processing = "PROCESSING";
    public const string Finished = "FINISHED";
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";
    // public const string Refunded = "REFUNDED"; // Cân nhắc thêm vào sau

    public static readonly List<StatusDefinition> All = new()
    {
        new(Pending, "Chờ xác nhận", "Đơn hàng mới tạo, chờ thanh toán đầy đủ hoặc chờ xử lý thanh toán.", [Processing, Cancelled]),
       // new(Deposited, "Đã đặt cọc", "Đã nhận tiền cọc, đơn hàng đủ điều kiện để bắt đầu sản xuất."),
        new(Processing, "Đang xử lý", "Các sản phẩm trong đơn đang được sản xuất, thiết kế, in hoặc soạn hàng.", [Finished, Completed]),
        new(Finished, "Chờ giao hàng", "Sản phẩm đã hoàn thành, chuẩn bị giao hàng hoặc chờ vận chuyển.", [Completed]),
        new(Completed, "Hoàn thành", "Khách hàng đã nhận hàng và hoàn tất thanh toán 100%.", []),
        new(Cancelled, "Đã hủy", "Đơn hàng bị hủy bởi khách hàng, nhân viên/quản lý hoặc hệ thống hết hạn thanh toán.", []),
        // new(Refunded, "Đã hoàn tiền", "Đơn hàng đã được hoàn lại tiền cho khách hàng.") 
    };
}
