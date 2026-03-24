using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
using sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class FeedbackEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feedback")
                       .WithTags("Feedback")
                       .WithOpenApi();

        group.MapPost("/my-pending", GetMyPending)
                .WithSummary("[Customer] Lấy danh sách các món hàng (OrderItem) đã nhận nhưng chưa đánh giá.");

        group.MapPost("/send", SendFeedback)
                .WithSummary("[Customer] Gửi đánh giá mới (kèm tối đa 5 ảnh).");

        group.MapGet("/my-history", GetMyHistory)
                .WithSummary("[Customer] Xem lại các đánh giá đã gửi.");
        group.MapGet("/template/{templateId}", GetFeedbackByTemplateId)
                .WithSummary("[All] Xem danh sách feedback của một mẫu thiết kế (kèm paging, filter theo số sao).");
        group.MapGet("/query", QueryFeedbacks)
                .WithSummary("[Staff/Manager] Lấy toàn bộ feedback để kiểm duyệt (Paging, Search).");
        group.MapPatch("/{id}/reply", ReplyFeedback)
                .WithSummary("[Staff/Manager] Nhân viên phản hồi đánh giá của khách.");

        group.MapPatch("/{id}/toggle-status", SwitchStatus)
                .WithSummary("[Staff/Manager] Ẩn/Hiện đánh giá (nếu vi phạm quy tắc cộng đồng).")
                .WithDescription("Để trống dữ liệu Request BODY khi gọi");


    }
    public async Task<IResult> GetMyPending([FromServices] ISender sender, [FromBody] GetPendingFeedbacksQuery query)
    {
        try
        {
            var result = await sender.Send(query);

            return TypedResults.Ok(BaseResponseModel<IEnumerable<PendingFeedbackDTO>>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result.Items,
                    additionalData: new { paging = result.Metadata },
                    message: "Lấy danh sách thành công"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> SendFeedback([FromServices] ISender sender, [FromBody] CreateFeedbackCommand command)
    {
        try
        {
            var result = await sender.Send(command);

            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Gửi phản hồi thành công!"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> GetMyHistory([FromServices] ISender sender, [FromBody] GetMyFeedbackHistoryQuery query)
    {
        try
        {
            var result = await sender.Send(query);

            return TypedResults.Ok(BaseResponseModel<IEnumerable<FeedbackDTO>>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result.Items,
                    additionalData: new { paging = result.Metadata },
                    message: "Lấy danh sách thành công"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> GetFeedbackByTemplateId([FromServices] ISender sender, [FromBody] GetFeedbacksByTemplateWithPaginationQuery query)
    {
        try
        {
            var result = await sender.Send(query);

            return TypedResults.Ok(BaseResponseModel<IEnumerable<FeedbackDTO>>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result.Items,
                    additionalData: new { paging = result.Metadata },
                    message: "Lấy danh sách thành công"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> QueryFeedbacks([FromServices] ISender sender, [FromBody] GetFeedbacksWithPaginationQuery query)
    {
        try
        {
            var result = await sender.Send(query);

            return TypedResults.Ok(BaseResponseModel<IEnumerable<FeedbackDTO>>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result.Items,
                    additionalData: new { paging = result.Metadata },
                    message: "Lấy danh sách thành công"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> ReplyFeedback([FromServices] ISender sender , [FromRoute] Guid id, [FromBody] ReplyFeedbackCommand command)
    {
        try
        {
            var finalCommand = command with { Id = id };
            var result = await sender.Send(finalCommand);

            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Trả lời thành công."
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> SwitchStatus([FromServices] ISender sender, [FromRoute] Guid id )
    {
        try
        {
            var result = await sender.Send(new SwitchFeedbackStatusCommand { Id = id });

            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Đổi trạng thái thành công"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
}
