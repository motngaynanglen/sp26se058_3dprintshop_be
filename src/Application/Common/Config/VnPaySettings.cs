namespace sp26se058_3dprintshop_be.Application.Common.Config;

public class VnPaySettings
{
    public const string SectionName = "VNPay";

    /// <summary>Mã website (Terminal ID) — lấy tại https://sandbox.vnpayment.vn/</summary>
    public string TmnCode { get; set; } = string.Empty;

    /// <summary>Chuỗi bí mật hash — Sandbox Merchant Admin.</summary>
    public string HashSecret { get; set; } = string.Empty;

    /// <summary>URL tạo thanh toán. Sandbox mặc định paymentv2.</summary>
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    /// <summary>FE redirect sau khi khách thanh toán xong (VNPay gọi browser về đây → BE redirect tiếp).</summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>IPN URL (VNPay server gọi server-to-server). Để trống = dùng {ApiBase}/api/transaction/vnpay-ipn</summary>
    public string IpnUrl { get; set; } = string.Empty;

    /// <summary>Base URL API public (để build ReturnUrl/IPN nếu chưa cấu hình).</summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:5080";

    /// <summary>FE redirect sau khi BE xử lý vnpay-return.</summary>
    public string FrontendReturnUrl { get; set; } = "http://localhost:3000";

    public string Version { get; set; } = "2.1.0";
    public string Locale { get; set; } = "vn";
    public string CurrCode { get; set; } = "VND";
    public string OrderType { get; set; } = "other";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TmnCode)
        && !string.IsNullOrWhiteSpace(HashSecret);
}
