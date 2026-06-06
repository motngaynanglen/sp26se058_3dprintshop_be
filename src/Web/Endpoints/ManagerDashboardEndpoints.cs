using Microsoft.AspNetCore.Http.HttpResults;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.ManagerDashboard.Models;
using sp26se058_3dprintshop_be.Application.ManagerDashboard.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class ManagerDashboardEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/manager")
            .WithTags("Manager Dashboard")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/dashboard", GetDashboard)
            .WithSummary("[Manager/Admin] Báo cáo doanh thu, tồn kho vật liệu, phản hồi khách hàng.");
    }

    private static async Task<IResult> GetDashboard(ISender sender)
    {
        try
        {
            var result = await sender.Send(new GetManagerDashboardQuery());
            return TypedResults.Ok(BaseResponseModel<ManagerDashboardDto>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Lấy báo cáo dashboard thành công"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return TypedResults.Json(
                new BaseResponseModel(403, ResponseCodeConstants.FORBIDDEN, ex.Message),
                statusCode: StatusCodes.Status403Forbidden);
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                new BaseResponseModel(400, ResponseCodeConstants.FAILED, ex.Message),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
