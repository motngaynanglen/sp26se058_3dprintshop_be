using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public sealed class PublicFileStorageService : IPublicFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly FileUploadOptions _opts;
    private readonly IPublicFileBaseUrlResolver _baseUrlResolver;

    public PublicFileStorageService(
        IWebHostEnvironment env,
        IOptions<FileUploadOptions> options,
        IPublicFileBaseUrlResolver baseUrlResolver)
    {
        _env = env;
        _opts = options.Value;
        _baseUrlResolver = baseUrlResolver;
    }

    public async Task<string> SavePublicFileAsync(
        byte[] content,
        string fileName,
        string? contentType = null,
        string? subFolder = null,
        CancellationToken cancellationToken = default)
    {
        if (content is null || content.Length == 0)
            throw new ArgumentException("Nội dung file rỗng.");

        var safeName = SanitizeFileName(fileName);
        var storedName = $"{Guid.NewGuid():N}_{safeName}";

        var prefix = (_opts.RelativeWebPath ?? "/uploads/public").TrimEnd('/');
        if (!prefix.StartsWith('/'))
            prefix = "/" + prefix;

        var yyyy = DateTime.UtcNow.ToString("yyyy");
        var mm = DateTime.UtcNow.ToString("MM");

        var webRoot = string.IsNullOrEmpty(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;

        var relativeSegments = prefix.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (!string.IsNullOrWhiteSpace(subFolder))
        {
            relativeSegments.AddRange(
                subFolder.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        var physicalDir = Path.Combine(
            new[] { webRoot }.Concat(relativeSegments).Concat(new[] { yyyy, mm }).ToArray());
        Directory.CreateDirectory(physicalDir);

        var physicalPath = Path.Combine(physicalDir, storedName);
        await File.WriteAllBytesAsync(physicalPath, content, cancellationToken);

        var urlPath = "/" + string.Join("/", relativeSegments.Concat(new[] { yyyy, mm, storedName }));
        return $"{_baseUrlResolver.GetBaseUrl()}{urlPath}";
    }

    private static string SanitizeFileName(string name)
    {
        var baseName = Path.GetFileName(name);
        foreach (var c in Path.GetInvalidFileNameChars())
            baseName = baseName.Replace(c, '_');
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "file.glb";
        return baseName.Length > 120 ? baseName[..120] : baseName;
    }
}
