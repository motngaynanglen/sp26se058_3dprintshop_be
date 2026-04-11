using System;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Orders.Commands;
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
                .WithSummary("[Customer] Tạo đơn hàng với thông tin giỏ hàng.")
                .WithDescription("Trước mắt SourceType chỉ hỗ trợ Type IN_STOCK, sau này sẽ sửa lại body để phù hợp 2 flow. " + SourceTypes.ListString);

        group.MapGet("/{id}/detail", GetDetail)
                .WithSummary("[All] lấy thông tin chi tiết đơn hàng có ID.");
        group.MapPatch("/{id}/cancel", CancelOrder)
                .WithSummary("[Customer/Staff/Manager] Hủy đơn hàng có ID.")
                .WithDescription("Chỉ hỗ trợ đơn hàng chưa thanh toán, hoặc đã tạo link thanh toán nhưng chưa thanh toán.");

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

    public async Task<IResult> Query([FromServices] ISender sender, [FromBody] GetOrdersWithPaginationQuery query)
    {

        var result = await sender.Send(query);

        return TypedResults.Ok(BaseResponseModel<IEnumerable<OrderDTO>>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result.Items,
                additionalData: new { paging = result.Metadata },
                message: "Lấy danh sách thành công"
            ));

    }

    public async Task<IResult> GetDetail([FromServices] ISender sender, [FromRoute] Guid id)
    {

        var result = await sender.Send(new GetOrderDetailQuery { Id = id });
        return TypedResults.Ok(BaseResponseModel<OrderDTO>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Lấy chi tiết đơn hàng thành công"
            ));

    }
    public async Task<IResult> CheckOut([FromServices] ISender sender, [FromBody] CheckoutCommand command)
    {

        var result = await sender.Send(command);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Tạo đơn hàng thành công"
            ));

    }
    public async Task<IResult> CancelOrder([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] CancelOrderCommand command)
    {

        var finalCommand = command with { OrderId = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Hủy đơn hàng thành công"
            ));

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
