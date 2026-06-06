namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

/// <summary>
/// Gọi OpenRouter (vision) để lên kế hoạch scene, sau đó dựng file GLB trên server.
/// </summary>
public interface IOpenRouterGlbGenerationService
{
    Task<byte[]> GenerateGlbAsync(string prompt, string imageBase64, string imageMimeType, CancellationToken cancellationToken = default);
}
