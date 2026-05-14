using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Application.ShippingAddresses.Queries;
using sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class ShippingAddressEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shipping-address")
                       .WithTags("Shipping Address")
                       .WithOpenApi();

        group.MapPost("/add", Add)
                .WithSummary("[Customer] Thêm địa chỉ nhận hàng mới.");

        group.MapGet("/my", GetMy)
                .WithSummary("[Customer] Lấy danh sách địa chỉ.");

        group.MapPatch("/{id}/update", Update)
                .WithSummary("[Customer] Cập nhật địa chỉ của bản thân có ID.");

        group.MapDelete("/{id}/remove", Remove)
                .WithSummary("[Customer] Xóa địa chỉ có ID.")
                .WithDescription("Để trống dữ liệu Request BODY khi gọi");

    }

    public async Task<IResult> Add([FromServices] ISender sender, [FromBody] CreateShippingAddressCommand command)
    {

        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<ShippingAddressDTO>.OkResponseModel(
            data: result,
            message: "Thêm địa chỉ thành công!",
            code: ResponseCodeConstants.CREATED));

    }

    public async Task<IResult> GetMy([FromServices] ISender sender)
    {

        var result = await sender.Send(new GetMyShippingAddressesQuery());

        return TypedResults.Ok(
            BaseResponseModel<IEnumerable<ShippingAddressDTO>>
                .ListResponseModel(data: result)
            );


    }

    public async Task<IResult> Remove([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] DeleteShippingAddressCommand command)
    {

        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
            data: result,
            message: "Loại bỏ địa chỉ thành công!",
            code: ResponseCodeConstants.DELETED));


    }

    public async Task<IResult> Update([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateShippingAddressCommand command)
    {

        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);

        return TypedResults.Ok(BaseResponseModel<ShippingAddressDTO>.OkResponseModel(
            data: result,
            message: "Cập nhật địa chỉ thành công!",
            code: ResponseCodeConstants.UPDATED));


    }
}
