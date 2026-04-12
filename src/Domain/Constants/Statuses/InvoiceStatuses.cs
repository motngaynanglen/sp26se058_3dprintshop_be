using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;
public static class InvoiceStatuses
{
    public const string Unpaid = "UNPAID";
    public const string Paid = "PAID";
    public const string Cancelled = "CANCELLED";
    // public const string PartiallyPaid = "PARTIALLY_PAID"; // Cân nhắc thêm vào sau

    public static readonly List<StatusDefinition> All = new()
    {
        new(Unpaid, "Chưa thanh toán", "#F44336", "Hóa đơn chưa được thực hiện thanh toán."),
        new(Paid, "Đã thanh toán", "#4CAF50", "Hóa đơn đã thanh toán đầy đủ 100%."),
        new(Cancelled, "Đã hủy", "#4CAF50", "Hóa đơn đã bị hủy trước khi thanh toán."),
        /*
        new(PartiallyPaid, "Thanh toán một phần", "#FF9800", "Mới nhận một phần tiền hoặc tiền đặt cọc.")
        */
    };
}
