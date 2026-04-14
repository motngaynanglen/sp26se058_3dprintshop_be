using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;
public static class TransactionStatuses
{
    public const string Pending = "PENDING";     // Đang chờ xử lý (vừa tạo link PayOS)
    public const string Success = "SUCCESS";     // Giao dịch thành công
    public const string Failed = "FAILED";       // Thất bại (Lỗi cổng thanh toán/Sai chữ ký)
    public const string Cancelled = "CANCELLED"; // Đã hủy (Người dùng chủ động hủy hoặc Link hết hạn)
    //public const string Refunded = "REFUNDED";   // Đã hoàn tiền (Dùng cho các trường hợp trả hàng/lỗi in)

    // --- Định nghĩa danh sách hiển thị (Metadata cho Frontend) ---
    public static readonly List<StatusDefinition> All = new()
    {
        new(Pending, "Đang chờ", "Giao dịch đã khởi tạo và đang chờ xác nhận từ hệ thống thanh toán."),
        new(Success, "Thành công", "Giao dịch đã hoàn tất thành công."),
        new(Failed, "Thất bại", "Giao dịch bị lỗi trong quá trình thực hiện."),
        new(Cancelled, "Đã hủy", "Giao dịch đã bị hủy bởi người dùng hoặc hệ thống."),
        //new(Refunded, "Đã hoàn tiền", "#2196F3", "Tiền của giao dịch này đã được hoàn lại cho khách hàng.")
    };
}
