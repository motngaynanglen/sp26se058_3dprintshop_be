
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class AppSupportEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/app-support")
                        .WithTags("AppSupport")
                        .WithOpenApi();
        // API dành riêng cho hình ảnh (Preview, Avatar, Minh họa)
        group.MapGet("/presigned-image-url", GetPresignedImageUrl)
                .WithSummary("Lấy link upload ảnh (Max 5MB); Trước mắt không bắt giới hạn.")
                .WithDescription("(Hỗ trợ: .jpg, .png, .webp); Trước mắt không bắt giới hạn dung lượng, nhớ gửi đúng TÊN FILE kèm ĐUÔI để đăng kí đúng link.");

        // API dành cho file 3D nặng (GLB, STL)
        group.MapGet("/presigned-model-url", GetPresignedModelUrl)
                .WithSummary("Lấy link upload file 3D (Max 100MB)")
                .WithDescription("(Hỗ trợ: .glb, .stl); Trước mắt không bắt giới hạn dung lượng, nhớ gửi đúng TÊN FILE kèm ĐUÔI để đăng kí đúng link.");

        group.MapPost("/test-upload-to-b2", TestUploadToB2)
                .DisableAntiforgery();
    }
    public async Task<IResult> GetPresignedImageUrl([FromQuery] string fileName, [FromServices] IS3StorageService s3Service)
    {
        // Đăng ký giới hạn 5MB cho ảnh
        long maxImageSize = 5 * 1024 * 1024; 
        
        var uploadUrl = await s3Service.GetPresignedUploadUrlAsync(fileName, "images", maxImageSize);

        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            code: ResponseCodeConstants.SUCCESS,
            data: new { UploadUrl = uploadUrl, FileUrl = uploadUrl.Split('?')[0] },
            message: "Lấy link upload ảnh thành công (Hỗ trợ: jpg, png, webp)"
        ));

    }
    public async Task<IResult> GetPresignedModelUrl(
        [FromQuery] string fileName,
        [FromServices] IS3StorageService s3Service)
    {
        // Đăng ký giới hạn 100MB cho file 3D
        long maxModelSize = 100 * 1024 * 1024;

        var uploadUrl = await s3Service.GetPresignedUploadUrlAsync(fileName, "models", maxModelSize);

        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            code: ResponseCodeConstants.SUCCESS,
            data: new { UploadUrl = uploadUrl, FileUrl = uploadUrl.Split('?')[0] },
            message: "Lấy link upload file 3D thành công (Hỗ trợ: .glb)"
        ));
    }
    public async Task<IResult> TestUploadToB2([FromQuery] string uploadUrl, [FromForm] IFormFile file)
    {

        using var client = new HttpClient();

        // Đọc file vào Stream
        using var stream = file.OpenReadStream();
        var content = new StreamContent(stream);

        // BẮT BUỘC: Content-Type phải khớp (thường là image/jpeg hoặc image/png)
        // CẢI THIỆN: Lấy trực tiếp ContentType của file vừa chọn trên Swagger
        // Ví dụ: Nếu chọn file .png, nó sẽ tự là "image/png"
        // Nếu chọn .webp, nó sẽ tự là "image/webp"
        var mimeType = file.ContentType;

        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

        // Thực hiện lệnh PUT 
        var response = await client.PutAsync(uploadUrl, content);

        if (response.IsSuccessStatusCode)
        {
            return TypedResults.Ok(new
            {
                Message = "BE Test: Upload lên Backblaze thành công!",
                StatusCode = (int)response.StatusCode
            });
        }

        var errorDetail = await response.Content.ReadAsStringAsync();
        return TypedResults.BadRequest(new
        {
            Message = "B2 từ chối File",
            Detail = errorDetail
        });

    }
}
