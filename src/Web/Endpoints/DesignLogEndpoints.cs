
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
        group.MapPost("/request-adjustment", RequestAdjustment)
             .WithSummary("[Customer/Staff/Manager] Tạo yêu cầu hiệu chỉnh chờ duyệt")
             .WithDescription("Tạo DesignLog loại ADJUSTMENT_REQUEST ở trạng thái PENDING_REVIEW. Chưa trừ lượt hiệu chỉnh cho đến khi Staff/Manager duyệt.");
        group.MapPost("/{id}/review-adjustment", ReviewAdjustment)
             .WithSummary("[Staff/Manager] Duyệt hoặc từ chối yêu cầu hiệu chỉnh")
             .WithDescription("Nếu duyệt in-scope thì trừ một lượt hiệu chỉnh đã thanh toán bằng cập nhật có điều kiện; nếu từ chối/out-of-scope thì không trừ lượt và yêu cầu khách tạo nhánh/đơn mới.");
        group.MapGet("/{designWorkId}/getLogsByWork", Get)
             .WithSummary("[All Roles] Lấy danh sách log theo công việc thiết kế")
             .WithDescription("Lấy log theo kiểu chat. Lần đầu không truyền beforeLogId để lấy log mới nhất; khi load tin cũ hơn, truyền beforeLogId là id log cũ nhất đang có trên màn hình.");
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

    public async Task<IResult> CreateVersionUpdateLog(
        [FromServices] ISender sender,
        [FromServices] IHubContext<DesignWorkChatHub> hubContext,
        [FromBody] CreateVersionUpdateLogCommand request)
    {
        var result = await sender.Send(request);

        await hubContext.Clients
            .Group(DesignWorkChatHub.GetGroupName(result.DesignWorkId))
            .SendAsync(DesignWorkChatHub.ReceiveDesignLogEvent, result);

        return TypedResults.Ok(BaseResponseModel<DesignLogDTO>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Tạo log ghi chú nội bộ thành công"
            ));
    }

    public async Task<IResult> RequestAdjustment(
        [FromServices] ISender sender,
        [FromServices] IHubContext<DesignWorkChatHub> hubContext,
        [FromBody] CreateAdjustmentRequestLogCommand request)
    {
        var result = await sender.Send(request);

        await hubContext.Clients
            .Group(DesignWorkChatHub.GetGroupName(result.DesignWorkId))
            .SendAsync(DesignWorkChatHub.ReceiveDesignLogEvent, result);

        return TypedResults.Ok(BaseResponseModel<DesignLogDTO>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: result,
                message: "Tạo yêu cầu hiệu chỉnh thành công"
            ));
    }

    public async Task<IResult> ReviewAdjustment(
        [FromServices] ISender sender,
        [FromServices] IHubContext<DesignWorkChatHub> hubContext,
        [FromRoute] Guid id,
        [FromBody] ReviewAdjustmentRequestLogCommand request)
    {
        var result = await sender.Send(request with { Id = id });

        await hubContext.Clients
            .Group(DesignWorkChatHub.GetGroupName(result.DesignWorkId))
            .SendAsync(DesignWorkChatHub.ReceiveDesignLogEvent, result);

        return TypedResults.Ok(BaseResponseModel<DesignLogDTO>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Cập nhật trạng thái yêu cầu hiệu chỉnh thành công"
            ));
    }

    public async Task<IResult> Get(
        [FromServices] ISender sender,
        [FromRoute] Guid designWorkId,
        [FromQuery] Guid? beforeLogId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 30)
    {
        var result = await sender.Send(new GetDesignLogsByWorkQuery
        {
            DesignWorkId = designWorkId,
            BeforeLogId = beforeLogId,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        var oldestLog = result.Items.FirstOrDefault();
        var newestLog = result.Items.LastOrDefault();

        return TypedResults.Ok(BaseResponseModel<IEnumerable<DesignLogDTO>>.ListResponseModel(
                data: result.Items,
                additionalData: new
                {
                    pagination = result.Metadata,
                    cursor = new
                    {
                        beforeLogId,
                        nextBeforeLogId = oldestLog?.Id,
                        newestLogId = newestLog?.Id,
                        hasOlder = result.Items.Any() && result.Metadata.TotalCount > 0 && result.Metadata.HasNextPage
                    },
                    ordering = new
                    {
                        mode = "CHAT_CURSOR",
                        description = "First call returns newest logs. For older logs, pass beforeLogId as the oldest log id currently rendered. Items are returned oldest-to-newest."
                    }
                },
                successMessage: "Lấy danh sách log thành công.",
                emptyMessage: "Không tìm thấy log nào cho công việc thiết kế này."
            ));
    }
}
