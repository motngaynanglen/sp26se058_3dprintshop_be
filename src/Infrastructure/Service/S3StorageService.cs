using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Config;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using FluentValidation.Results;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;
public class S3StorageService : IS3StorageService
{
    private readonly BackblazeB2Settings _b2Settings;
    private readonly IAmazonS3 _s3Client;

    // Dictionary để map extension sang Content-Type chuẩn
    private readonly Dictionary<string, string> _allowedExtensions = new()
    {
        // Ảnh
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".webp", "image/webp" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".svg", "image/svg+xml" },
        // Model 3D
        { ".glb", "model/gltf-binary" },
        { ".stl", "application/vnd.ms-pki.stl" },
        { ".obj", "application/x-tgif" },
        { ".fbx", "application/octet-stream" },
        { ".3mf", "application/vnd.ms-package.3dmanufacturing-3dmodel+xml" },
    };

    public S3StorageService( IOptions<BackblazeB2Settings> b2Settings)
    {
        _b2Settings = b2Settings.Value;

        var s3Config = new AmazonS3Config
        {
            ServiceURL = _b2Settings.ServiceUrl,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(_b2Settings.KeyId, _b2Settings.ApplicationKey, s3Config);
    }

    public async Task<string> GetPresignedUploadUrlAsync(
        string fileName, 
        string folderName, 
        long maxSizeBytes, 
        int expiresMinutes = 15)
    {
        // Chặn dung lượng tối đa 1MB ( bắt sau )
        //const long maxAllowedSize = 1 * 1024 * 1024;
        //if (fileSize > maxAllowedSize)
        //    throw new Exception("File vượt quá giới hạn 5MB.");

        //var b2Config = _config.GetSection("BackblazeB2");
        // 1. Lấy đuôi file và kiểm tra tính hợp lệ
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ValidationException( new List<ValidationFailure> { 
                new ValidationFailure("fileName","Tên file không được để trống." )
            });
        }

        var extension = Path.GetExtension(fileName).ToLower();

        // 2. Kiểm tra định dạng có trong danh sách cho phép không
        if (!_allowedExtensions.TryGetValue(extension, out var contentType))
        {
            throw new BusinessException($"Định dạng file {extension} không được hỗ trợ. Hệ thống chỉ chấp nhận hình ảnh và file 3D (.glb).");
        }

        // 3. Tạo đường dẫn file: vd service-options/abc-123.jpg
        var key = $"{folderName}/{Guid.NewGuid()}_{fileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _b2Settings.BucketName,
            Key = key,
            Verb = HttpVerb.PUT, // Dùng PUT để upload
            Expires = DateTime.UtcNow.AddMinutes(expiresMinutes),
            ContentType = contentType
        };

        // Ký xác nhận Content-Length để B2 tự động chặn nếu upload sai dung lượng 
        // code truoc implement sau
        //request.Headers["Content-Length"] = maxSizeBytes.ToString();

        return await Task.Run(() => _s3Client.GetPreSignedURL(request));
    }
}
