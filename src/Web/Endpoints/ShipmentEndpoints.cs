using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Application.Shipping.Queries;
using sp26se058_3dprintshop_be.Application.ShippingAddresses.Queries;
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

        // --- NHÓM TRUY VẤN (QUERIES) ---
        group.MapPost("/query", QueryShipments)
            .WithSummary("[Staff/Manager] Truy vấn danh sách vận đơn.")
            .WithDescription("Hỗ trợ tìm kiếm, lọc theo trạng thái và phân trang. Sắp xếp: 'Tracking', 'Fee', 'Created', 'Shipped', 'Delivered'.");

        group.MapGet("/{id}/detail", GetById)
            .WithSummary("[All] Lấy thông tin chi tiết vận đơn theo ID.")
            .WithDescription("Khách hàng chỉ xem được vận đơn của mình. Staff/Manager xem được tất cả.");

        group.MapGet("/{orderId}/detail-by-order-id", GetByOrderId)
            .WithSummary("[All] Lấy thông tin vận đơn thông qua Order ID.");

        // --- NHÓM ĐIỀU PHỐI TRẠNG THÁI (COMMANDS - THEO LUỒNG) ---

        group.MapPatch("/{id}/mark-ready", MarkReady)
            .WithSummary("[Staff] Xác nhận đóng gói xong, chờ gửi hàng.")
            .WithDescription("Chuyển trạng thái sang 'ReadyForPickup'. Chỉ thực hiện được khi toàn bộ vật phẩm trong đơn đã sản xuất xong.");

        group.MapPatch("/{id}/mark-in-transit", StartInTransit)
            .WithSummary("[Staff] Xác nhận đã giao cho đơn vị vận chuyển.")
            .WithDescription("Chuyển trạng thái sang 'InTransit'. Yêu cầu nhập CarrierName và TrackingNumber.");

        group.MapPatch("/{id}/confirm-delivered", ConfirmDelivered)
            .WithSummary("[Staff] Xác nhận shipper đã giao hàng thành công.")
            .WithDescription("Chuyển trạng thái sang 'Delivered'. Ghi nhận thời điểm giao hàng để tính thời hạn bảo hành/khiếu nại.");

        group.MapPatch("/{id}/mark-failed", MarkFailed)
            .WithSummary("[Staff] Báo cáo sự cố giao hàng thất bại.")
            .WithDescription("Chuyển trạng thái sang 'Failed'. Yêu cầu nhập lý do để làm căn cứ xử lý (giao lại hoặc hoàn hàng).");

        group.MapPatch("/{id}/mark-returning", MarkReturning)
            .WithSummary("[Staff] Xác nhận kiện hàng đang chuyển hoàn.")
            .WithDescription("Chuyển trạng thái sang 'Returning'. Áp dụng khi kiện hàng đang giao hoặc đã giao thất bại.");

        group.MapPatch("/{id}/confirm-returned", ConfirmReturned)
            .WithSummary("[Staff] Xác nhận đã nhận hàng hoàn.")
            .WithDescription("Chuyển trạng thái từ 'Returning' sang 'Returned'.");

        group.MapPatch("/{id}/mark-lost-or-damaged", MarkLostOrDamaged)
            .WithSummary("[Staff/Manager] Ghi nhận kiện hàng thất lạc hoặc hư hỏng.")
            .WithDescription("Chuyển trạng thái từ 'Returning' sang 'LostOrDamaged'.");

        group.MapPatch("/{id}/cancel", Cancel)
            .WithSummary("[Staff/Manager] Hủy lượt vận chuyển.")
            .WithDescription("Chỉ hủy vận đơn, không hủy đơn hàng/hóa đơn. Áp dụng khi chưa bàn giao cho đơn vị vận chuyển.");

        group.MapPost("/{id}/address-change-requests", RequestAddressChange)
            .WithSummary("[Customer] Gửi yêu cầu đổi địa chỉ giao hàng.")
            .WithDescription("Khách hàng chỉ gửi yêu cầu; staff/manager xác minh thực tế rồi duyệt hoặc từ chối.");

        group.MapPatch("/address-change-requests/{id}/approve", ApproveAddressChange)
            .WithSummary("[Staff/Manager] Duyệt yêu cầu đổi địa chỉ giao hàng.")
            .WithDescription("Chỉ duyệt nếu vận đơn chưa được bàn giao cho đơn vị vận chuyển.");

        group.MapPatch("/address-change-requests/{id}/reject", RejectAddressChange)
            .WithSummary("[Staff/Manager] Từ chối yêu cầu đổi địa chỉ giao hàng.");

        // --- NHÓM CẬP NHẬT HÀNH CHÍNH ---
        group.MapPatch("/Add", Create)
            .WithSummary("[Staff/Manager] Tạo mới một vận đơn.")
            .WithDescription("Phòng trường hợp đơn gửi trước đó bị lỗi hoặc gặp vấn đề. Có thể tạo mới một lượt vận chuyển khác.");

        // --- GHN SHIPPING ---
        group.MapPost("/quotes", GetShippingQuotes)
            .WithSummary("[All] Lấy báo giá phí vận chuyển từ các hãng");

        group.MapGet("/ghn/provinces", GetGhnProvinces)
            .AllowAnonymous()
            .WithSummary("[Public] Danh sách tỉnh/thành GHN");

        group.MapGet("/ghn/districts", GetGhnDistricts)
            .AllowAnonymous()
            .WithSummary("[Public] Danh sách quận/huyện theo tỉnh GHN");

        group.MapGet("/ghn/wards", GetGhnWards)
            .AllowAnonymous()
            .WithSummary("[Public] Danh sách phường/xã theo quận GHN");

        group.MapPost("/order/{orderId}/create-carrier", CreateCarrierShipment)
            .WithSummary("[Staff/Manager] Tạo vận đơn GHN");

        group.MapGet("/{id}/label", GetCarrierLabel)
            .WithSummary("[Staff/Manager] Lấy phiếu in (printA5) cho vận đơn GHN");

        group.MapPost("/{id}/sync-carrier", SyncCarrierShipment)
            .WithSummary("[Staff/Manager] Đồng bộ trạng thái từ đơn vị vận chuyển");

        group.MapPost("/webhook/ghn", HandleGhnWebhook)
            .AllowAnonymous()
            .WithSummary("[GHN] Webhook cập nhật trạng thái vận chuyển");

    }
    public async Task<IResult> QueryShipments([FromServices] ISender sender, [FromBody] GetShipmentsWithPaginationQuery command)
    {
        var result = await sender.Send(command);

        return TypedResults.Ok(
            BaseResponseModel<IEnumerable<ShipmentDTO>>
                .ListResponseModel(data: result.Items, additionalData: new { paging = result.Metadata })
                );
    }
    public async Task<IResult> Create([FromServices] ISender sender, [FromBody] CreateShipmentCommand command)
    {


        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: result,
            message: "Thêm địa chỉ thành công!",
            code: ResponseCodeConstants.CREATED));

    }
    public async Task<IResult> GetByOrderId([FromServices] ISender sender, [FromRoute] Guid orderId)
    {
        var result = await sender.Send(new GetShipmentByOrderIdQuery { OrderId = orderId });

        return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
            data: result,
            message: "Truy vấn thành công!",
            code: ResponseCodeConstants.SUCCESS));
    }
    public async Task<IResult> GetById([FromServices] ISender sender, [FromRoute] Guid id)
    {

        var result = await sender.Send(new GetShipmentByIdQuery { Id = id });

        return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
            data: result,
            message: "Truy vấn thành công!",
            code: ResponseCodeConstants.SUCCESS));

    }
    public async Task<IResult> MarkReady([FromServices] ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new MarkShipmentAsReadyCommand { Id = id });
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: result,
            message: "Kiện hàng đã sẵn sàng để giao!",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> StartInTransit([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] MarkShipmentAsInTransitCommand command)
    {
        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: result,
            message: "Đã bắt đầu quá trình vận chuyển.",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> ConfirmDelivered([FromServices] ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new ConfirmShipmentDeliveredCommand { Id = id });
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: result,
            message: "Giao hàng thành công!",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> MarkFailed([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] MarkShipmentAsFailedCommand command)
    {
        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: result,
            message: "Đã ghi nhận sự cố giao hàng.",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> MarkReturning([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] MarkShipmentAsReturningCommand command)
    {
        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
            data: result,
            message: "Đã ghi nhận kiện hàng đang chuyển hoàn.",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> ConfirmReturned([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] ConfirmShipmentReturnedCommand command)
    {
        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
            data: result,
            message: "Đã xác nhận nhận hàng hoàn.",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> MarkLostOrDamaged([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] MarkShipmentAsLostOrDamagedCommand command)
    {
        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
            data: result,
            message: "Đã ghi nhận kiện hàng thất lạc hoặc hư hỏng.",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> Cancel([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] CancelShipmentCommand command)
    {
        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
            data: result,
            message: "Đã hủy lượt vận chuyển.",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> RequestAddressChange([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] RequestShipmentAddressChangeCommand command)
    {
        var finalCommand = command with { ShipmentId = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<ShipmentAddressChangeRequestDTO>.OkResponseModel(
            data: result,
            message: "Đã gửi yêu cầu đổi địa chỉ giao hàng.",
            code: ResponseCodeConstants.CREATED));
    }

    public async Task<IResult> ApproveAddressChange([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] ApproveShipmentAddressChangeCommand command)
    {
        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<ShipmentAddressChangeRequestDTO>.OkResponseModel(
            data: result,
            message: "Đã duyệt yêu cầu đổi địa chỉ giao hàng.",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> RejectAddressChange([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] RejectShipmentAddressChangeCommand command)
    {
        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<ShipmentAddressChangeRequestDTO>.OkResponseModel(
            data: result,
            message: "Đã từ chối yêu cầu đổi địa chỉ giao hàng.",
            code: ResponseCodeConstants.UPDATED));
    }

    // --- GHN Shipping Handlers ---

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

    public async Task<IResult> GetCarrierLabel([FromServices] ISender sender, [FromRoute] Guid id)
    {
        try
        {
            var result = await sender.Send(new GetCarrierShipmentLabelQuery { Id = id });
            return TypedResults.Ok(BaseResponseModel<CarrierShipmentLabelResult>.OkResponseModel(
                data: result,
                message: "OK",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Không lấy được phiếu in vận đơn."));
        }
    }

    public async Task<IResult> SyncCarrierShipment([FromServices] ISender sender, [FromRoute] Guid id)
    {
        try
        {
            var result = await sender.Send(new SyncCarrierShipmentCommand { Id = id });
            return TypedResults.Ok(BaseResponseModel<ShipmentDTO>.OkResponseModel(
                data: result,
                message: "Đã đồng bộ trạng thái vận chuyển.",
                code: ResponseCodeConstants.UPDATED));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Không đồng bộ được trạng thái vận chuyển."));
        }
    }

    public async Task<IResult> HandleGhnWebhook(
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
            return dict;
        }

        // GHN bắn webhook bằng body JSON (Content-Type: application/json) — phải đọc body,
        // không chỉ Form/Query. Flatten các field cấp 1 ra dictionary (giữ raw cho object/array).
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var raw = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return dict;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                        ? prop.Value.GetRawText()
                        : prop.Value.ToString();
                }
            }
        }
        catch (JsonException)
        {
            // Body không phải JSON hợp lệ — bỏ qua, đã có Query phía trên.
        }

        return dict;
    }

    public record CreateCarrierShipmentBody(
        string Carrier,
        int? WeightGrams,
        int? GhnDistrictId = null,
        string? GhnWardCode = null);
}
