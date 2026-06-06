namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

/// <summary>Chuẩn hóa URL file Mainflow 2 để khách/NV xem được trên trình duyệt.</summary>
public interface IMainflow2AccessibleFileUrlService
{
    string? Resolve(string? fileUrl);

    string? RewriteQuoteMetadataJson(string? metadataJson);

    /// <summary>Flow AI: báo giá dùng GLB khách, không dùng template khối demo.</summary>
    string? RewriteStaffQuoteMetadataForDisplay(string? metadataJson, string? sourceType, string? customerFileUrl);
}
