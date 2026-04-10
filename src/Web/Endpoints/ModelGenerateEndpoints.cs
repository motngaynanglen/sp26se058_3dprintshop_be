using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.ModelGeneratorAI.Command;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class ModelGenerateEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/model-generate")
                       .WithTags("Model Generate")
                       .WithOpenApi();
        group.MapPost("", Generate)
     .WithSummary("Tạo mô hình 3D từ ảnh")
     .DisableAntiforgery()                    // Quan trọng khi dùng IFormFile trong .NET 8+
     .Accepts<GenerateModelCommand>("multipart/form-data");  // Buộc dùng multipart
    }

    public async Task<IResult> Generate(
    [FromServices] ISender sender,
    [FromForm] IFormFile? Image)   // ← Truyền trực tiếp file, không qua Command
    {
        if (Image == null || Image.Length == 0)
            return TypedResults.BadRequest("Vui lòng chọn file ảnh");

        try
        {
            // Tạo command từ file
            var command = new GenerateModelCommand { Image = Image };

            var resultUrl = await sender.Send(command);

            return TypedResults.Ok(BaseResponseModel<string>.OkResponseModel(
                data: resultUrl,
                message: "Tạo mô hình 3D thành công!",
                code: ResponseCodeConstants.SUCCESS
            ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Tạo mô hình 3D thất bại!"
            ));
        }
    }
}
