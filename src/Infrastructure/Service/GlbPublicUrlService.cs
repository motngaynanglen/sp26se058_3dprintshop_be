using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Options;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public sealed class GlbPublicUrlService : IGlbPublicUrlService
{
    private readonly IBackblazeB2Service _b2;
    private readonly IPublicFileStorageService _local;
    private readonly BackblazeB2Options _b2Opts;
    private readonly GlbPublishOptions _publishOpts;

    public GlbPublicUrlService(
        IBackblazeB2Service b2,
        IPublicFileStorageService local,
        IOptions<BackblazeB2Options> b2Options,
        IOptions<GlbPublishOptions> publishOptions)
    {
        _b2 = b2;
        _local = local;
        _b2Opts = b2Options.Value;
        _publishOpts = publishOptions.Value;
    }

    public async Task<string> PublishGlbAsync(byte[] glbData, string folder = "models", CancellationToken cancellationToken = default)
    {
        if (glbData is null || glbData.Length == 0)
            throw new ArgumentException("GLB rỗng.");

        if (UseBackblaze())
        {
            try
            {
                return await _b2.UploadGlbAsync(glbData, folder);
            }
            catch
            {
                // B2 lỗi → fallback local public URL
            }
        }

        return await _local.SavePublicFileAsync(
            glbData,
            $"{Guid.NewGuid():N}.glb",
            "model/gltf-binary",
            folder,
            cancellationToken);
    }

    private bool UseBackblaze() =>
        string.Equals(_publishOpts.Storage, "Backblaze", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(_b2Opts.KeyId)
        && !string.IsNullOrWhiteSpace(_b2Opts.ApplicationKey)
        && !string.IsNullOrWhiteSpace(_b2Opts.BucketName)
        && !string.IsNullOrWhiteSpace(_b2Opts.ServiceUrl);
}
