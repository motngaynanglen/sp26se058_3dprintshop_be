using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Orders.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class FeedbackEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feedback")
                       .WithTags("Feedbacks")
                       .WithOpenApi();

        group.MapGet("/my-pending", GetMyPending)
                .WithSummary("[Customer] Lấy danh sách các món hàng (OrderItem) đã nhận nhưng chưa đánh giá.");

        group.MapPost("/send", SendFeedback)
                .WithSummary("[Customer] Gửi đánh giá mới (kèm tối đa 5 ảnh).");

        group.MapGet("/my-history", GetMyHistory)
                .WithSummary("[Customer] Xem lại các đánh giá đã gửi.");
        group.MapGet("/template/{templateId}", GetFeedbackByTemplateId)
                .WithSummary("[All] Xem danh sách feedback của một mẫu thiết kế (kèm paging, filter theo số sao).");
        group.MapGet("/query", QueryShipments)
                .WithSummary("[Staff/Manager] Lấy toàn bộ feedback để kiểm duyệt (Paging, Search).");
        group.MapPatch("/{id}/reply", ReplyFeedback)
                .WithSummary("[Staff/Manager] Nhân viên phản hồi đánh giá của khách.");
        group.MapPatch("/{id}/toggle-status", SwitchStatus)
                .WithSummary("[Staff/Manager] Ẩn/Hiện đánh giá (nếu vi phạm quy tắc cộng đồng).");

    }
    public Task<IResult> GetMyPending([FromServices] ISender sender)
    {
        throw new NotImplementedException();
    }
    public Task<IResult> SendFeedback([FromServices] ISender sender)
    {
        throw new NotImplementedException();
    }
    public Task<IResult> GetMyHistory([FromServices] ISender sender)
    {
        throw new NotImplementedException();
    }
    public Task<IResult> GetFeedbackByTemplateId([FromServices] ISender sender)
    {
        throw new NotImplementedException();
    }
    public Task<IResult> QueryShipments([FromServices] ISender sender)
    {
        throw new NotImplementedException();
    }
    public Task<IResult> ReplyFeedback([FromServices] ISender sender)
    {
        throw new NotImplementedException();
    }
    public Task<IResult> SwitchStatus([FromServices] ISender sender)
    {
        throw new NotImplementedException();
    }
}
