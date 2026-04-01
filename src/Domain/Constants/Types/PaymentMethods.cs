using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Types;
public static class PaymentMethods
{
    //public const string BankTransfer = "BANK_TRANSFER";
    public const string PAYOS = "PAYOS";
    //public const string MoMo = "MOMO"; // Cân nhắc thêm vào sau
    //public const string Cash = "CASH";
    //public const string VNPAY = "VNPAY"; 

    public static readonly List<StatusDefinition> All = new()
    {
        //new(BankTransfer, "Chuyển khoản ngân hàng", "#00468C", "Thanh toán qua số tài khoản ngân hàng chính thức."),
        new(PAYOS, "Ví điện tử PAYOS", "#A50064", "Thanh toán nhanh qua ứng dụng PAYOS."),
        //new(VNPAY, "Ví điện tử VNPAY", "#A50064", "Thanh toán nhanh qua ứng dụng VNPAY."),
        //new(Cash, "Tiền mặt", "#4CAF50", "Thanh toán trực tiếp tại cửa hàng hoặc khi nhận hàng."),
    };
}
