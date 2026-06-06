namespace sp26se058_3dprintshop_be.Application.Common.Config;

/// <summary>Nơi lưu GLB AI / model-generate và URL trả về cho FE (model-viewer).</summary>
public class GlbPublishOptions
{
    public const string SectionName = "GlbPublish";

    /// <summary><c>Local</c> = wwwroot + PublicBaseUrl; <c>Backblaze</c> = B2 (bucket phải public hoặc dùng CDN).</summary>
    public string Storage { get; set; } = "Local";
}
