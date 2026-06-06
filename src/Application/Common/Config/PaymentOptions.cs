namespace sp26se058_3dprintshop_be.Application.Common.Config;

/// <summary>Cấu hình thanh toán — tách biệt catalog (thu đủ) và đơn custom MF2 (cọc + phần còn lại).</summary>
public class PaymentOptions
{
    public const string SectionName = "Payment";

    /// <summary>Phần trăm đặt cọc cho đơn custom Mainflow2 (in theo yêu cầu). Catalog luôn thu 100%.</summary>
    public decimal CustomOrderDepositPercent { get; set; } = 30m;
}
