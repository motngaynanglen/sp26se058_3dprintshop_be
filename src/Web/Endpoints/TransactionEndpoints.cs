using BG_IMPACT.Business.Command.Transaction.Commands;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using PayOS.Models.Webhooks;
using sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
using sp26se058_3dprintshop_be.Application.Auths.Commands.Register;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Transaction.Command;
using sp26se058_3dprintshop_be.Application.Transaction.Commands;
using sp26se058_3dprintshop_be.Application.Transaction.Queries;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Infrastructure.Identity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class TransactionEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transaction")
                       .WithTags("Transaction")
                       .WithOpenApi()
                       .RequireCors("AllowFrontend"); // Ép Swagger phải nhận diện group này

        group.MapPost("/payos-webhook", HandlePayOSWebhook)
                    .WithSummary("[PayOS] Đây là api để payos gọi để xác thực đã thanh toán");

        group.MapPost("/perform-transaction", PerformTransaction)
                    .WithSummary("[All] Gửi yêu cầu thanh toán đơn hàng có ID.");
        group.MapPost("/{id}/cancel", CancelTransaction)
                   .WithSummary("[All] Gửi yêu cầu thanh toán đơn hàng có ID.");
        group.MapGet("/{orderId}/detail-by-order-id", GetDetailByOrderID)
                    .WithSummary("[All] Tìm chi tiết giao dịch bằng ID hàng hóa.");



    }

    public async Task<IResult> HandlePayOSWebhook([FromServices] ISender sender, [FromBody] ProcessOnlinePaymentCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel.OkResponseModel(
                    data: result,
                    message: "Xác nhực thanh toán thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            // Trả về 401 Unauthorized
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> PerformTransaction([FromServices] ISender sender, [FromBody] PerformTransactionCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel.OkResponseModel(
                    data: result,
                    message: "Mở cổng thanh toán thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            // Trả về 401 Unauthorized
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> GetDetailByOrderID([FromServices] ISender sender, [FromRoute] Guid orderId)
    {
        try
        {
            var result = await sender.Send(new GetTransactionByOrderIdQuery { OrderId = orderId});
            return TypedResults.Ok(BaseResponseModel.OkResponseModel(
                    data: result,
                    message: "Đã tìm thấy cổng thanh toán!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            
            // Trả về 401 Unauthorized
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> CancelTransaction([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] CancelTransactionCommand command)
    {
        try
        {
            var finalcommand = command with { TransactionId = id };
            var result = await sender.Send(finalcommand);
            return TypedResults.Ok(BaseResponseModel.OkResponseModel(
                    data: result,
                    message: "Mở cổng thanh toán thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            // Trả về 401 Unauthorized
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
}
