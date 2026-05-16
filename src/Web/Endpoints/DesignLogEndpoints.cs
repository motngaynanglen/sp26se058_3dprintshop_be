
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.DesignLogs.Commands;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using sp26se058_3dprintshop_be.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class DesignLogEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/design-log")
                       .WithTags("Design Log")
                       .WithOpenApi();
        group.MapPost("/createChatLog", CreateChatLog)
             .WithSummary("[Staff/Manager] Tạo log giao tiếp")
             .WithDescription("Tạo log giao tiếp liên quan đến công việc thiết kế. Chỉ dành cho Staff, Manager, Customer.");
        group.MapPost("/createNewVersionUpdateLog", CreateVersionUpdateLog)
             .WithSummary("[Staff/Manager] Tạo log ghi chú nội bộ")
             .WithDescription("Cập nhật phiên bản mới của thiết kế. Chỉ dành cho Staff hoặc Manager.");
        group.MapGet("/{designWorkId}/getLogsByWork", Get)
             .WithSummary("[All Roles] Lấy danh sách log theo công việc thiết kế")
             .WithDescription("Lấy danh sách log liên quan đến một công việc thiết kế. Dành cho tất cả các vai trò.");
    }

    public async Task<IResult> Query([FromServices] ISender sender, [FromBody] GetPaginationDesignWorkQuery request)
    {
        var result = await sender.Send(request);
        return TypedResults.Ok(BaseResponseModel<IEnumerable<DesignWorkDTO>>.ListResponseModel(
                data: result.Items,
                additionalData: new { pagination = result.Metadata },
                successMessage: "Lấy danh sách thành công.",
                emptyMessage: "Không tìm thấy kết quả nào phù hợp."
            ));
    }

    public async Task<IResult> CreateChatLog(
        [FromServices] ISender sender,
        [FromServices] IHubContext<DesignWorkChatHub> hubContext,
        [FromBody] CreateDesignLogCommand request)
    {
        var result = await sender.Send(request);

        await hubContext.Clients
            .Group(DesignWorkChatHub.GetGroupName(result.DesignWorkId))
            .SendAsync(DesignWorkChatHub.ReceiveDesignLogEvent, result);

        return TypedResults.Ok(BaseResponseModel<DesignLogDTO>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Tạo log giao tiếp thành công"
            ));
    }

    public async Task<IResult> CreateVersionUpdateLog([FromServices] ISender sender, [FromBody] CreateVersionUpdateLogCommand request)
    {
        var result = await sender.Send(request);
        return TypedResults.Ok(BaseResponseModel<DesignLogDTO>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Tạo log ghi chú nội bộ thành công"
            ));
    }

    public async Task<IResult> Get([FromServices] ISender sender, [FromRoute] Guid designWorkId)
    {
        var result = await sender.Send(new GetDesignLogsByWorkQuery(designWorkId));
        return TypedResults.Ok(BaseResponseModel<IEnumerable<DesignLogDTO>>.ListResponseModel(
                data: result,
                successMessage: "Lấy danh sách log thành công.",
                emptyMessage: "Không tìm thấy log nào cho công việc thiết kế này."
            ));
    }
}
