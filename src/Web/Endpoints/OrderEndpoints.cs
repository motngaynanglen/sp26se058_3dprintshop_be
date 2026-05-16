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


        // --- TRUY VẤN ---
        group.MapPost("/query", Query)
                    .WithSummary("[All] Truy vấn danh sách đơn hàng.");

        group.MapGet("/{id}/detail", GetDetail)
                    .WithSummary("[All] Lấy thông tin chi tiết đơn hàng.");


        // --- FLOW KHỞI TẠO & HỦY ---
        group.MapPost("/checkout", CheckOut)
                .WithSummary("[Customer] Tạo đơn hàng mới từ giỏ hàng.")
                .WithDescription("Hỗ trợ các loại nguồn hàng: " + SourceTypes.InStock +", " + SourceTypes.PreOrder);

        group.MapPost("/checkout-design", CheckOutDesignAsync)
                .WithSummary("[Customer] Tạo đơn hàng mới từ thiết kế.")
                .WithDescription("Hỗ trợ các loại nguồn hàng: " + SourceTypes.DesignService);

        group.MapPost("/checkout-draft", CheckOutDraft)
                .WithSummary("[Customer] Tạo đơn sản xuất các bản thiết kế từ dịch vụ thiết kế.")
                .WithDescription("Hỗ trợ các loại nguồn hàng: " + SourceTypes.PrintService);

        group.MapPatch("/{id}/cancel", CancelOrder)
                .WithSummary("[Customer/Staff/Manager] Hủy đơn hàng.")
                .WithDescription("Chỉ hỗ trợ khi đơn hàng chưa thanh toán hoặc chưa đi vào sản xuất.");

        group.MapPost("/sync-expired-pending-cancellations", SyncExpiredPendingOrders)
                .WithSummary("[Staff/Manager] Đồng bộ hủy các đơn hàng chờ thanh toán đã quá hạn.")
                .WithDescription("Chủ động quét các đơn PENDING đã quá hạn thanh toán, hủy đơn, hủy giao dịch, hủy vận đơn và hoàn kho nếu cần.");

        // --- FLOW SẢN XUẤT & HOÀN TẤT (NEW) ---

        group.MapPatch("/item/{orderItemId}/finish-package", FinishOrderItem)
                .WithSummary("[Staff] Xác nhận một món hàng vật lý đã làm xong.")
                .WithDescription("Chuyển trạng thái Item sang FINISHED. Nếu là món cuối cùng, hệ thống tự động cập nhật Shipment sang READY_FOR_PICKUP.");

        group.MapPatch("/{id}/complete", CompleteOrder)
                .WithSummary("[Customer/Staff/Manager] Hoàn thành đơn hàng (Đóng đơn).")
                .WithDescription("Khách có thể bấm bất cứ lúc nào sau khi nhận hàng. Nhân viên/quản lý chỉ được bấm sau 3 ngày kể từ khi đơn được giao thành công.");

       
    }

    public async Task<IResult> Query([FromServices] ISender sender, [FromBody] GetOrdersWithPaginationQuery query)
    {

        var result = await sender.Send(query);

        return TypedResults.Ok(BaseResponseModel<IEnumerable<OrderDTO>>.ListResponseModel(
                data: result.Items,
                additionalData: new { paging = result.Metadata },
                successMessage: "Lấy danh sách đơn hàng thành công.",
                emptyMessage: "Không tìm thấy đơn hàng nào."
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
                code: ResponseCodeConstants.CREATED,
                data: result,
                message: "Tạo đơn hàng thành công"
            ));

    }

    public async Task<IResult> CheckOutDesignAsync([FromServices] ISender sender, [FromBody] CheckoutDesignWorkCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: result,
                message: "Tạo đơn hàng từ thiết kế"
            ));
    }
    public async Task<IResult> CheckOutDraft([FromServices] ISender sender, [FromBody] CheckoutDraftsCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: result,
                message: "Tạo đơn hàng thành công."
            ));
    }
    public async Task<IResult> CancelOrder([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] CancelOrderCommand command)
    {

        var finalCommand = command with { OrderId = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: result,
                message: "Hủy đơn hàng thành công"
            ));

    }

    public async Task<IResult> SyncExpiredPendingOrders([FromServices] ISender sender)
    {
        var result = await sender.Send(new SyncExpiredPendingOrdersCommand());
        return TypedResults.Ok(BaseResponseModel<SyncExpiredPendingOrdersResultDTO>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: result,
                message: result.CancelledCount > 0
                    ? $"Đã đồng bộ hủy {result.CancelledCount} đơn hàng chờ thanh toán quá hạn."
                    : "Không có đơn hàng chờ thanh toán quá hạn cần hủy."
            ));
    }

    public async Task<IResult> FinishOrderItem([FromServices] ISender sender, [FromRoute] Guid orderItemId)
    {
        var result = await sender.Send(new MarkOrderItemAsFinishedPackageCommand { Id = orderItemId});
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: result,
                message: "Xác nhận món hàng đã hoàn tất sản xuất/đóng gói."
            ));
    }

    // API mới chốt vòng đời đơn hàng
    public async Task<IResult> CompleteOrder([FromServices] ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new CompleteOrderCommand { Id = id });
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: result,
                message: "Đơn hàng đã hoàn thành và đóng lại thành công."
            ));
    }
}
