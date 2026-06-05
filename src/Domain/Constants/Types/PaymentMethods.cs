using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Types;
public static class PaymentMethods
{
    public const string PAYOS = "PAYOS";
    public const string VNPAY = "VNPAY";
    public const string Cash = "CASH";
    public const string BankTransfer = "BANK_TRANSFER";

    public static readonly List<StatusDefinition> All = new()
    {
        new(PAYOS, "Ví điện tử PAYOS", "Thanh toán nhanh qua ứng dụng PAYOS."),
        new(VNPAY, "Cổng thanh toán VNPay", "Thanh toán qua cổng VNPay (ATM/Visa/MasterCard/QR)."),
        new(Cash, "Tiền mặt", "Thanh toán trực tiếp tại cửa hàng hoặc khi nhận hàng."),
        new(BankTransfer, "Chuyển khoản ngân hàng", "Thanh toán qua số tài khoản ngân hàng chính thức."),
    };
}
