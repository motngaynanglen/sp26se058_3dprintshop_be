using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Options;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class BackblazeB2Service : IBackblazeB2Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _serviceUrl;

    public BackblazeB2Service(IOptions<BackblazeB2Options> options)
    {
        var config = options.Value;
        var s3Config = new AmazonS3Config
        {
            ServiceURL = config.ServiceUrl,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(config.KeyId, config.ApplicationKey, s3Config);
        _bucketName = config.BucketName;
        _serviceUrl = config.ServiceUrl.TrimEnd('/');
    }

    public async Task<string> UploadGlbAsync(byte[] glbBytes, string folder = "models")
    {
        var fileName = $"{folder.Trim('/')}/{Guid.NewGuid():N}.glb";
        using var stream = new MemoryStream(glbBytes);
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = "model/gltf-binary"
        };

        await _s3Client.PutObjectAsync(request);
        return $"{_serviceUrl}/{_bucketName}/{fileName}";
    }

    public string GetPresignedDownloadUrl(string objectUrl, TimeSpan? expires = null)
    {
        if (string.IsNullOrWhiteSpace(objectUrl))
            return objectUrl;

        var key = ExtractObjectKey(objectUrl);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expires ?? TimeSpan.FromHours(1))
        };

        return _s3Client.GetPreSignedURL(request);
    }

    private string ExtractObjectKey(string storedUrl)
    {
        if (!storedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return storedUrl.TrimStart('/');

        var uri = new Uri(storedUrl);
        var segments = uri.AbsolutePath.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && string.Equals(segments[0], _bucketName, StringComparison.Ordinal))
            return string.Join('/', segments.Skip(1));

        return uri.AbsolutePath.TrimStart('/');
    }
}
