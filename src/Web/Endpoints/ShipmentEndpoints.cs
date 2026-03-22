using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Application.ShippingAddress.Queries;
using sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class ShipmentEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shipment")
                       .WithTags("Shipment")
                       .WithOpenApi();

        //group.MapPost("/add", Add)
        //        .WithSummary("[Customer] Thêm địa chỉ nhận hàng mới.");

        //group.MapGet("/my", GetMy)
        //        .WithSummary("[Customer] Lấy danh sách địa chỉ.");
        group.MapPost("/query", QueryShipments)
            .WithSummary("[Staff/Manager] Truy vấn danh sách tài khoản.")
            .WithDescription("Hỗ trợ tìm kiếm, lọc và phân trang danh sách tài khoản trong hệ thống. Nếu data null nghĩa là mặc định lấy hết. Xắp xếp hỗ trợ: 'Tracking', 'Fee', 'Created', 'Shipped', 'Delivered' ");

        group.MapPatch("/update/{id}", Update)
                .WithSummary("[Staff/Manager] Cập nhật thông tin đơn vận có ID.");

        group.MapGet("/get-by-order-id/{orderId}", GetByOrderId)
                .WithSummary("[All] Lấy thông tin đơn vận có đơn hàng ID.");
        group.MapGet("/get-by-id/{id}", GetById)
                .WithSummary("[Customer/Staff/Manager] Lấy thông tin đơn vận có ID.")
                .WithDescription("Bảo mật: Nếu là Customer, chỉ cho phép xem vận đơn của chính họ\n Manager và Staff có quyền xem mọi vận đơn");


    }
    public async Task<IResult> QueryShipments([FromServices] ISender sender, [FromBody] GetShipmentsWithPaginationQuery command)
    {
        try
        {
            var result = await sender.Send(command);

            return TypedResults.Ok(BaseResponseModel<IEnumerable<ShipmentDTO>>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result.Items,
                    additionalData: new { paging = result.Metadata },
                    message: "Lấy danh sách thành công"
                ));
        }
        catch (UnauthorizedAccessException)
        {
            // Trả về 401 Unauthorized
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(null, code: ResponseCodeConstants.INVALID_CREDENTIALS),
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }
    public async Task<IResult> Update([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateShipmentCommand command)
    {
        try
        {
            var finalCommand = command with { Id = id };
            var result = await sender.Send(finalCommand);

            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                data: result,
                message: "Cập nhật địa chỉ thành công!",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Lỗi trong quá trình cập nhật."
            ));
        }
    }
    public async Task<IResult> GetByOrderId([FromServices] ISender sender, [FromRoute] Guid orderId)
    {
        try
        {
            var result = await sender.Send(new GetShipmentByOrderIdQuery { OrderId = orderId });

            return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
                data: result,
                message: "Truy vấn thành công!",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Lỗi trong quá trình truy vấn."
            ));
        }
    }
    public async Task<IResult> GetById([FromServices] ISender sender, [FromRoute] Guid id)
    {
        try
        {
            var result = await sender.Send(new GetShipmentByIdQuery { Id = id });

            return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
                data: result,
                message: "Truy vấn thành công!",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Lỗi trong quá trình truy vấn."
            ));
        }
    }
}
