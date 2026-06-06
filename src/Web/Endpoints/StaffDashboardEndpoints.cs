using Microsoft.AspNetCore.Http.HttpResults;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.StaffDashboard.Models;
using sp26se058_3dprintshop_be.Application.StaffDashboard.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class StaffDashboardEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/staff")
            .WithTags("Staff")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/workbench", GetWorkbench)
            .WithSummary("[Staff/Manager] Bàn làm việc KTV — việc ưu tiên từ dữ liệu thật (DB).");
    }

    private static async Task<IResult> GetWorkbench(ISender sender)
    {
        try
        {
            var result = await sender.Send(new GetStaffWorkbenchQuery());
            return TypedResults.Ok(BaseResponseModel<StaffWorkbenchDto>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Lấy bàn làm việc thành công"));
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
