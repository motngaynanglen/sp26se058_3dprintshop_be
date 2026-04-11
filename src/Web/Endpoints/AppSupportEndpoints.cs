
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
        group.MapGet("/presigned-upload-url", GetPresignedUploadUrl)
                .WithSummary("[Staff/Manager] Lấy link đăng kí trước cho upload");

        group.MapPost("/test-upload-to-b2", TestUploadToB2)
                .WithSummary("[Dev Only] Test đẩy file trực tiếp lên B2 bằng Presigned URL.")
                .DisableAntiforgery();
    }
    public async Task<IResult> GetPresignedUploadUrl([FromQuery] string fileName, [FromServices] IS3StorageService s3Service)
    {
        try
        {
            var uploadUrl = await s3Service.GetPresignedUploadUrlAsync(fileName, "sp26se058");

            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                    data: new
                    {
                        UploadUrl = uploadUrl,
                        // Trả về luôn URL đích để Frontend lưu vào DB sau khi upload xong
                        FileUrl = uploadUrl.Split('?')[0]
                    },
                    message: "Lấy URL thành công!"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> TestUploadToB2([FromQuery] string uploadUrl,[FromForm] IFormFile file)
    {
        try
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
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
}
