using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;
public class S3StorageService : IS3StorageService
{
    private readonly IConfiguration _config;
    private readonly IAmazonS3 _s3Client;

    // Dictionary để map extension sang Content-Type chuẩn
    private readonly Dictionary<string, string> _allowedExtensions = new()
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".webp", "image/webp" }
    };

    public S3StorageService(IConfiguration config)
    {
        _config = config;
        var b2Config = _config.GetSection("BackblazeB2");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = b2Config["ServiceUrl"],
            ForcePathStyle = true // Quan trọng đối với B2
        };

        _s3Client = new AmazonS3Client(b2Config["KeyId"], b2Config["ApplicationKey"], s3Config);
    }

    public async Task<string> GetPresignedUploadUrlAsync(string fileName, string folderName, int expiresMinutes = 15)
    {

        var b2Config = _config.GetSection("BackblazeB2");
        // 1. Lấy đuôi file và kiểm tra tính hợp lệ
        if (string.IsNullOrEmpty(fileName)) throw new Exception("Tên file không được để trống.");

        var extension = Path.GetExtension(fileName).ToLower();

        // 2. Kiểm tra định dạng có trong danh sách cho phép không
        if (!_allowedExtensions.TryGetValue(extension, out var contentType))
        {
            throw new Exception($"Định dạng file {extension} không được hỗ trợ. Chỉ chấp nhận: png, jpg, jpeg, webp.");
        }

        // 2. Tạo đường dẫn file: vd service-options/abc-123.jpg
        var key = $"{folderName}/{Guid.NewGuid()}_{fileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = b2Config["BucketName"],
            Key = key,
            Verb = HttpVerb.PUT, // Dùng PUT để upload
            Expires = DateTime.UtcNow.AddMinutes(expiresMinutes),
            ContentType = contentType
        };

        return await Task.Run(() => _s3Client.GetPreSignedURL(request));
    }
}
