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
        new(Pending, "Chờ xác nhận", "#9E9E9E", "Đơn hàng mới tạo, chờ nhân viên xác nhận hoặc chờ khách đặt cọc."),
       // new(Deposited, "Đã đặt cọc", "#FF9800", "Đã nhận tiền cọc, đơn hàng đủ điều kiện để bắt đầu sản xuất."),
        new(Processing, "Đang xử lý", "#2196F3", "Các sản phẩm trong đơn đang được sản xuất hoặc thiết kế."),
        new(Finished, "Chờ giao hàng", "#00BCD4", "Sản phẩm đã hoàn thành, chuẩn bị giao hàng hoặc chờ thanh toán nốt."),
        new(Completed, "Hoàn thành", "#4CAF50", "Khách hàng đã nhận hàng và hoàn tất thanh toán 100%."),
        new(Cancelled, "Đã hủy", "#F44336", "Đơn hàng bị hủy bởi khách hàng hoặc quản trị viên."),
        // new(Refunded, "Đã hoàn tiền", "#E91E63", "Đơn hàng đã được hoàn lại tiền cho khách hàng.") 
    };
}
