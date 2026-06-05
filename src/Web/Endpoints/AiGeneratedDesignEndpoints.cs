using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.AiDesign.Commands;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class AiGeneratedDesignEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai-generated-design")
            .WithTags("AI Generated Design")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/register", Register)
            .WithSummary("[Customer] Đăng ký mô hình AI để KTV kiểm tra và báo giá")
            .WithDescription("Khách upload GLB từ AI → gọi endpoint này → tạo DesignWork loại Quick Print → KTV review.");
    }

    private static async Task<IResult> Register(ISender sender, [FromBody] RegisterAiGeneratedDesignWorkCommand body)
    {
        var id = await sender.Send(body);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            code: ResponseCodeConstants.CREATED,
            data: id,
            message: "Đã gửi yêu cầu in từ mô hình AI. KTV sẽ kiểm tra file và báo giá."));
    }
}
