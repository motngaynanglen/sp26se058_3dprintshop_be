namespace sp26se058_3dprintshop_be.Application.Common.Config;

/// <summary>
/// Cấu hình endpoint <c>POST /api/files/upload</c>. Lấy từ section <c>FileUpload</c> trong appsettings.json.
/// </summary>
public class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    /// <summary>Kích thước tối đa (byte) cho 1 file. 0 hoặc âm = không giới hạn.</summary>
    public long MaxBytes { get; set; }

    /// <summary>Đường dẫn web tương đối (bắt đầu bằng /). VD: <c>/uploads/public</c>.</summary>
    public string RelativeWebPath { get; set; } = "/uploads/public";

    /// <summary>Danh sách extension cho phép, ngăn cách bằng dấu phẩy (ví dụ <c>.stl,.obj,.glb</c>).</summary>
    public string AllowedExtensions { get; set; } = ".png,.jpg,.jpeg,.webp,.gif,.glb,.obj,.stl";

    /// <summary>URL gốc BE để ghép publicUrl (VD <c>http://localhost:5080</c>). Rỗng → dùng request host khi upload qua API.</summary>
    public string? PublicBaseUrl { get; set; }
}
