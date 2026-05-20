using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
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
}
