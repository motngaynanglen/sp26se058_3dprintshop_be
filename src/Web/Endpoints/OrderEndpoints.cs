using System;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Orders.Commands;
using sp26se058_3dprintshop_be.Application.Orders.Models;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class OrderEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/order")
                       .WithTags("Order")
                       .WithOpenApi();
        group.MapPost("/query", Query)
                    .WithSummary("[All] Truy vấn danh sách đơn hàng.");

        group.MapPost("/checkout", CheckOut)
                .WithSummary("[Customer] Tạo đơn hàng (thống nhất 3 luồng).")
                .WithDescription(
                    "IN_STOCK / PRE_ORDER: Items[].DesignVariantId. " +
                    "CUSTOM_QUOTE_MF2: sau khi khách duyệt báo giá — Items[].DesignWorkId. " +
                    "AI_GENERATED: sau POST /api/ai-generated-design/register — Items[].DesignWorkId. " +
                    "Luôn cần ShippingAddressId. Sau checkout: thanh toán + tracking qua chi tiết đơn. " +
                    "SourceType: " + SourceTypes.ListString);

        group.MapGet("/{id}/detail", GetDetail)
                .WithSummary("[All] lấy thông tin chi tiết đơn hàng có ID.");
        group.MapPatch("/{id}/cancel", CancelOrder)
                .WithSummary("[Customer/Staff/Manager] Hủy đơn hàng có ID.")
                .WithDescription("Chỉ hỗ trợ đơn hàng chưa thanh toán, hoặc đã tạo link thanh toán nhưng chưa thanh toán.");
        group.MapPatch("/{id}/status", UpdateStatus)
                .WithSummary("[Staff/Manager] Cập nhật trạng thái đơn hàng + shipment + tracking number.")
                .WithDescription("OrderStatus: PROCESSING / FINISHED / COMPLETED. ShipmentStatus (tùy chọn): PREPARING / READY_FOR_PICKUP / IN_TRANSIT / DELIVERED.")
                .RequireAuthorization();

        group.MapPost("/production-queue", GetProductionQueue)
                .WithSummary("[Staff/Manager] Hàng đợi sản xuất (flow 2/3, pre-order).")
                .RequireAuthorization();

        group.MapPatch("/items/{orderItemId}/fulfillment", UpdateItemFulfillment)
                .WithSummary("[Staff/Manager] Cập nhật tiến độ in / hoàn thiện từng dòng hàng.")
                .RequireAuthorization();

        //group.MapPut("/update/{id}", Update);

        /*
        của customer
        group.MapPost("/add", Add); //Tạo đơn hàng mới
        group.MapPost("/invoice", CreateInvoice); // Tạo hóa đơn cho đơn hàng
        group.MapPut("/query", Query); // Truy vấn đơn hàng với phân trang
        group.MapGet("/detail/{id}", GetById); // Lấy chi tiết đơn hàng

        của staff
        group.MapPost("/confirm", ConfirmOrder); // Xác nhận đơn hàng
        group.MapPatch("/update-status/{id}", UpdateStatus); // Cập nhật trạng thái đơn hàng (ví dụ: đang in, đã hoàn thành, đã giao hàng)
         */
    }

    public async Task<IResult> Query ([FromServices] ISender sender, [FromBody] GetOrdersWithPaginationQuery query)
    {
        try
        {
            var result = await sender.Send(query);

            return TypedResults.Ok(BaseResponseModel<IEnumerable<OrderDTO>>.OkResponseModel(
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

    public async Task<IResult> GetDetail([FromServices] ISender sender, [FromRoute] Guid id)
    {
        try
        {
            var result = await sender.Send(new GetOrderDetailQuery { Id = id });
            return TypedResults.Ok(BaseResponseModel<OrderDTO>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Lấy chi tiết đơn hàng thành công"
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);

        }
    }
    public async Task<IResult> CheckOut([FromServices] ISender sender, [FromBody] CheckoutCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Tạo đơn hàng thành công"
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);

        }
    }
    public async Task<IResult> CancelOrder([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] CancelOrderCommand command)
    {
        try
        {
            var finalCommand = command with { OrderId = id };
            var result = await sender.Send(finalCommand);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Hủy đơn hàng thành công"
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);

        }
    }

    public async Task<IResult> GetProductionQueue(
        [FromServices] ISender sender,
        [FromBody] GetProductionQueueQuery query)
    {
        try
        {
            var result = await sender.Send(query);
            return TypedResults.Ok(BaseResponseModel<IEnumerable<ProductionQueueOrderDto>>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result.Items,
                additionalData: new { paging = result.Metadata },
                message: "Lấy hàng đợi sản xuất thành công."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public async Task<IResult> UpdateItemFulfillment(
        [FromServices] ISender sender,
        [FromRoute] Guid orderItemId,
        [FromBody] UpdateOrderItemFulfillmentBody body)
    {
        try
        {
            var result = await sender.Send(new UpdateOrderItemFulfillmentCommand
            {
                OrderItemId = orderItemId,
                FulfillmentStatus = body.FulfillmentStatus,
                Note = body.Note
            });
            return TypedResults.Ok(BaseResponseModel<UpdateOrderItemFulfillmentResult>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: result.Message ?? "Cập nhật tiến độ sản xuất thành công."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public record UpdateOrderItemFulfillmentBody(string FulfillmentStatus, string? Note);

    public async Task<IResult> UpdateStatus([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateOrderStatusCommand command)
    {
        try
        {
            var finalCommand = command with { OrderId = id };
            var result = await sender.Send(finalCommand);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: result,
                message: "Cập nhật trạng thái đơn hàng thành công."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
    //public async Task<IResult> Create(ISender sender)
    //{
    //    return TypedResults.Ok();
    //}

    //public async Task<IResult> Update(ISender sender)
    //{
    //    var order = await sender.Send(sender);
    //    return TypedResults.Ok();
    //}
}
