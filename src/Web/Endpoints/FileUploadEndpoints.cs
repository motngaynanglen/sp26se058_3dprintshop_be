using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class FileUploadEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files")
            .WithTags("Files")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/upload", UploadPublicFile)
            .WithSummary("[Auth] Upload file — lưu dưới wwwroot, trả URL tĩnh công khai.")
            .DisableAntiforgery();
    }

    private static async Task<IResult> UploadPublicFile(
        IFormFile? file,
        IPublicFileStorageService storage,
        IOptions<FileUploadOptions> options,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return TypedResults.BadRequest("Vui lòng chọn file.");

        var opts = options.Value;
        if (opts.MaxBytes > 0 && file.Length > opts.MaxBytes)
            return TypedResults.BadRequest($"File vượt quá giới hạn {opts.MaxBytes} bytes.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = (opts.AllowedExtensions ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : "." + e.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (allowed.Count > 0 && !allowed.Contains(ext))
            return TypedResults.BadRequest($"Đuôi file không được phép: {ext}");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var url = await storage.SavePublicFileAsync(
            ms.ToArray(),
            file.FileName,
            file.ContentType,
            cancellationToken: cancellationToken);

        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: new { url, fileName = file.FileName, size = file.Length },
            message: "Upload thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }
}
