namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

/// <summary>Lưu file dưới wwwroot và trả URL công khai (phục vụ qua UseStaticFiles).</summary>
public interface IPublicFileStorageService
{
    /// <summary>Trả URL đầy đủ (http://host/uploads/...).</summary>
    Task<string> SavePublicFileAsync(
        byte[] content,
        string fileName,
        string? contentType = null,
        string? subFolder = null,
        CancellationToken cancellationToken = default);
}
