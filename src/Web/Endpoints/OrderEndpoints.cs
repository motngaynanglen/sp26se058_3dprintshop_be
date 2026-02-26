using System;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class OrderEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/order")
                       .WithTags("Order")
                       .WithOpenApi();
        group.MapPost("/query", Query);
        //group.MapPost("/add", Create);
        group.MapGet("/detail/{id}", GetDetail);
        //group.MapPut("/update/{id}", Update);

        /*
        của customer
        group.MapPost("/add", Add); //Tạo đơn hàng mới
        group.MapPost("/invoice", CreateInvoice); // Tạo hóa đơn cho đơn hàng
        group.MapPut("/query", Query); // Truy vấn đơn hàng với phân trang
        group.MapGet("/detail/{id}", GetById); // Lấy chi tiết đơn hàng
        group.MapPost("/cancel", CancelOrder); // Hủy đơn hàng

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
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(null, code: ResponseCodeConstants.INVALID_CREDENTIALS),
                statusCode: StatusCodes.Status401Unauthorized);
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
        catch (Exception)
        {
            return TypedResults.NotFound(BaseResponseModel<object>.NotFoundResponseModel(null));

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
