using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Application.Shipping.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class ShipmentEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shipment")
            .WithTags("Shipment")
            .WithOpenApi();

        group.MapPost("/query", Query)
            .WithSummary("[Staff/Manager] Truy vấn danh sách vận đơn.");

        group.MapPatch("/{id}/update", Update)
            .WithSummary("[Staff/Manager] Cập nhật vận đơn.");

        group.MapGet("/{orderId}/detail-by-order-id", GetByOrderId)
            .WithSummary("[All] Lấy vận đơn theo OrderId.");

        group.MapGet("/{id}/detail", GetById)
            .WithSummary("[Customer/Staff/Manager] Lấy thông tin đơn vận có ID.")
            .WithDescription("Bảo mật: Customer chỉ xem vận đơn của mình; Staff/Manager xem mọi vận đơn.");

        group.MapPost("/quotes", GetShippingQuotes)
            .WithSummary("[Customer] Báo phí GHN/GHTK theo địa chỉ.")
            .WithDescription("Trả phí thật nếu đã cấu hình Token API; không thì phí ước tính (FallbackFee).");

        group.MapGet("/ghn/provinces", GetGhnProvinces)
            .WithSummary("[Customer] Danh sách tỉnh/thành GHN.")
            .AllowAnonymous();

        group.MapGet("/ghn/districts", GetGhnDistricts)
            .WithSummary("[Customer] Danh sách quận/huyện GHN theo tỉnh.")
            .AllowAnonymous();

        group.MapGet("/ghn/wards", GetGhnWards)
            .WithSummary("[Customer] Danh sách phường/xã GHN theo quận.")
            .AllowAnonymous();

        group.MapPost("/order/{orderId}/create-carrier", CreateCarrierShipment)
            .WithSummary("[Staff/Manager] Tạo vận đơn GHN hoặc GHTK.")
            .RequireAuthorization();

        group.MapPost("/webhook/ghn", GhnWebhook)
            .WithSummary("[GHN] Webhook cập nhật trạng thái vận đơn.")
            .AllowAnonymous();

        group.MapPost("/webhook/ghtk", GhtkWebhook)
            .WithSummary("[GHTK] Webhook cập nhật trạng thái vận đơn.")
            .AllowAnonymous();
    }

    public async Task<IResult> Query(
        [FromServices] ISender sender,
        [FromBody] GetShipmentsWithPaginationQuery query)
    {
        try
        {
            var result = await sender.Send(query);
            return TypedResults.Ok(BaseResponseModel<IEnumerable<ShipmentDTO>>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result.Items,
                additionalData: new { paging = result.Metadata },
                message: "Lấy danh sách vận đơn thành công."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public async Task<IResult> Update(
        [FromServices] ISender sender,
        [FromRoute] Guid id,
        [FromBody] UpdateShipmentCommand command)
    {
        try
        {
            command = command with { Id = id };
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                data: result,
                message: "Cập nhật vận đơn thành công.",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public async Task<IResult> GetByOrderId(
        [FromServices] ISender sender,
        [FromRoute] Guid orderId)
    {
        try
        {
            var result = await sender.Send(new GetShipmentByOrderIdQuery { OrderId = orderId });
            return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
                data: result,
                message: "Lấy vận đơn thành công.",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    public async Task<IResult> GetById(
        [FromServices] ISender sender,
        [FromRoute] Guid id)
    {
        try
        {
            var result = await sender.Send(new GetShipmentByIdQuery { Id = id });
            return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
                data: result,
                message: "Lấy chi tiết vận đơn thành công.",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    public async Task<IResult> GetShippingQuotes(
        [FromServices] ISender sender,
        [FromBody] GetShippingQuotesQuery query)
    {
        try
        {
            var result = await sender.Send(query);
            return TypedResults.Ok(BaseResponseModel<List<ShippingQuoteDto>>.OkResponseModel(
                data: result,
                message: "Báo phí vận chuyển thành công.",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Không tính được phí vận chuyển."));
        }
    }

    public async Task<IResult> GetGhnProvinces([FromServices] IGhnMasterDataService ghn)
    {
        try
        {
            var list = await ghn.GetProvincesAsync();
            return TypedResults.Ok(BaseResponseModel<IReadOnlyList<GhnProvinceDto>>.OkResponseModel(
                data: list,
                message: "OK",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(ex.Message));
        }
    }

    public async Task<IResult> GetGhnDistricts(
        [FromServices] IGhnMasterDataService ghn,
        [FromQuery] int provinceId)
    {
        try
        {
            var list = await ghn.GetDistrictsAsync(provinceId);
            return TypedResults.Ok(BaseResponseModel<IReadOnlyList<GhnDistrictDto>>.OkResponseModel(
                data: list,
                message: "OK",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(ex.Message));
        }
    }

    public async Task<IResult> GetGhnWards(
        [FromServices] IGhnMasterDataService ghn,
        [FromQuery] int districtId)
    {
        try
        {
            var list = await ghn.GetWardsAsync(districtId);
            return TypedResults.Ok(BaseResponseModel<IReadOnlyList<GhnWardDto>>.OkResponseModel(
                data: list,
                message: "OK",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(ex.Message));
        }
    }

    public async Task<IResult> CreateCarrierShipment(
        [FromServices] ISender sender,
        [FromRoute] Guid orderId,
        [FromBody] CreateCarrierShipmentBody body)
    {
        try
        {
            var result = await sender.Send(new CreateCarrierShipmentCommand
            {
                OrderId = orderId,
                Carrier = body.Carrier,
                WeightGrams = body.WeightGrams,
                GhnDistrictId = body.GhnDistrictId,
                GhnWardCode = body.GhnWardCode
            });

            return TypedResults.Ok(BaseResponseModel<CreateCarrierShipmentResult>.OkResponseModel(
                data: result,
                message: $"Đã tạo vận đơn {result.Carrier}.",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Tạo vận đơn thất bại."));
        }
    }

    public async Task<IResult> GhnWebhook(
        [FromServices] ISender sender,
        HttpRequest httpRequest)
    {
        var payload = await ReadFormOrQueryAsync(httpRequest);
        var ok = await sender.Send(new ProcessCarrierWebhookCommand
        {
            Carrier = "GHN",
            Payload = payload
        });
        return TypedResults.Ok(new { success = ok });
    }

    public async Task<IResult> GhtkWebhook(
        [FromServices] ISender sender,
        HttpRequest httpRequest)
    {
        var payload = await ReadFormOrQueryAsync(httpRequest);
        var ok = await sender.Send(new ProcessCarrierWebhookCommand
        {
            Carrier = "GHTK",
            Payload = payload
        });
        return TypedResults.Ok(new { success = ok });
    }

    private static async Task<Dictionary<string, string>> ReadFormOrQueryAsync(HttpRequest request)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in request.Query)
            dict[q.Key] = q.Value.ToString();

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            foreach (var f in form)
                dict[f.Key] = f.Value.ToString();
        }

        return dict;
    }

    public record CreateCarrierShipmentBody(
        string Carrier,
        int? WeightGrams,
        int? GhnDistrictId = null,
        string? GhnWardCode = null);
}
