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
    }

    // Đổi tên tham số từ glbBase64 thành glbData để phản ánh đúng kiểu byte[]
    public async Task<string> UploadGlbAsync(byte[] glbData, string folder = "models")
    {
        var fileName = $"{folder}/{Guid.NewGuid():N}.glb";

        using var stream = new MemoryStream(glbData);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = "model/gltf-binary" // Định nghĩa chuẩn cho file .glb
        };

        await _s3Client.PutObjectAsync(request);

        // Lưu ý: Thay us-east-005 bằng Region thực tế của Bucket bạn nếu có lỗi URL
        return $"https://{_bucketName}.s3.us-east-005.backblazeb2.com/{fileName}";
    }
}
