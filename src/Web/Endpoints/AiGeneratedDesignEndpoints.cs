using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.AiDesign.Commands;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

/// <summary>Luồng 3: sau khi /api/model-generate trả URL GLB, đăng ký yêu cầu báo giá KTV (giống flow 2).</summary>
public class AiGeneratedDesignEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai-generated-design")
            .WithTags("AI generated design")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/register", Register)
            .WithSummary("[Customer] Đăng ký mô hình AI (URL file + giá) để đặt hàng qua /api/order/checkout.");
    }

    private static async Task<IResult> Register(ISender sender, [FromBody] RegisterAiGeneratedDesignWorkCommand body)
    {
        try
        {
            var id = await sender.Send(body);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: id,
                message: "Đã gửi yêu cầu in từ mô hình AI. KTV sẽ báo giá — theo dõi tại custom orders."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message),
                statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<object>.BadRequestResponseModel(ex.Message));
        }
    }
}
