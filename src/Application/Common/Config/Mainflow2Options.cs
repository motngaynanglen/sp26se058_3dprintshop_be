namespace sp26se058_3dprintshop_be.Application.Common.Config;

/// <summary>Cấu hình lưu GLB preview báo giá Mainflow 2.</summary>
public class Mainflow2Options
{
    public const string SectionName = "Mainflow2";

    /// <summary>
    /// <c>Local</c> — ghi vào wwwroot, URL công khai qua static files.
    /// <c>Backblaze</c> — upload B2 (cần cấu hình hợp lệ).
    /// </summary>
    public string QuoteGlbStorage { get; set; } = "Local";

    /// <summary>Tiền tố URL (bắt đầu bằng /), ví dụ /uploads/mainflow2</summary>
    public string LocalPublicPathPrefix { get; set; } = "/uploads/mainflow2";
}
