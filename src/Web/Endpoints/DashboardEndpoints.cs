using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Dashboards.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class DashboardEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .WithOpenApi();

        group.MapGet("/manager", GetManagerDashboard)
            .WithSummary("[Manager] Tổng quan vận hành toàn hệ thống.")
            .WithDescription("Trả về số lượng theo trạng thái, doanh thu, tồn kho thấp, các cảnh báo cần xử lý và đơn gần đây.");

        group.MapGet("/manager/status-summary", GetManagerStatusSummary)
            .WithSummary("[Manager] Số lượng theo trạng thái.")
            .WithDescription("Cụm nhẹ cho các biểu đồ trạng thái đơn hàng, item, vận đơn, thiết kế và hóa đơn.");

        group.MapGet("/manager/revenue", GetManagerRevenue)
            .WithSummary("[Manager] Tổng quan doanh thu.")
            .WithDescription("Cụm doanh thu đã thanh toán, doanh thu tháng hiện tại và công nợ chưa thanh toán.");

        group.MapGet("/manager/action-items", GetManagerActionItems)
            .WithSummary("[Manager] Các cảnh báo cần xử lý.")
            .WithDescription("Cụm việc cần chú ý như đơn quá hạn, yêu cầu đổi địa chỉ, vận đơn lỗi và tồn kho thấp.");

        group.MapGet("/manager/inventory", GetManagerInventory)
            .WithSummary("[Manager] Tồn kho cần chú ý.")
            .WithDescription("Cụm danh sách biến thể tồn kho thấp.");

        group.MapGet("/manager/recent-orders", GetManagerRecentOrders)
            .WithSummary("[Manager] Đơn hàng gần đây.");

        group.MapGet("/staff", GetStaffDashboard)
            .WithSummary("[Staff/Manager] Hàng đợi công việc vận hành.")
            .WithDescription("Staff xem các đơn/thiết kế được gán và việc cần xử lý. Manager có thể dùng để xem hàng đợi vận hành chung.");

        group.MapGet("/staff/summary", GetStaffSummary)
            .WithSummary("[Staff/Manager] Tóm tắt công việc theo trạng thái.");

        group.MapGet("/staff/work-queue", GetStaffWorkQueue)
            .WithSummary("[Staff/Manager] Hàng đợi thao tác cần xử lý.");

        group.MapGet("/staff/recent-work", GetStaffRecentWork)
            .WithSummary("[Staff/Manager] Đơn và thiết kế gần đây.");
    }

    public async Task<IResult> GetManagerDashboard([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetManagerDashboardQuery());
        return TypedResults.Ok(BaseResponseModel<ManagerDashboardDTO>.OkResponseModel(
            data: result,
            message: "Lấy dashboard quản lý thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetStaffDashboard([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetStaffDashboardQuery());
        return TypedResults.Ok(BaseResponseModel<StaffDashboardDTO>.OkResponseModel(
            data: result,
            message: "Lấy dashboard nhân viên thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetManagerStatusSummary([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetManagerDashboardStatusSummaryQuery());
        return TypedResults.Ok(BaseResponseModel<ManagerDashboardStatusSummaryDTO>.OkResponseModel(
            data: result,
            message: "Lấy tóm tắt trạng thái dashboard thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetManagerRevenue([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetManagerDashboardRevenueQuery());
        return TypedResults.Ok(BaseResponseModel<ManagerDashboardRevenueSummaryDTO>.OkResponseModel(
            data: result,
            message: "Lấy tóm tắt doanh thu dashboard thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetManagerActionItems([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetManagerDashboardActionItemsQuery());
        return TypedResults.Ok(BaseResponseModel<ManagerDashboardActionItemsDTO>.OkResponseModel(
            data: result,
            message: "Lấy danh sách cảnh báo dashboard thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetManagerInventory([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetManagerDashboardInventoryQuery());
        return TypedResults.Ok(BaseResponseModel<ManagerDashboardInventorySummaryDTO>.OkResponseModel(
            data: result,
            message: "Lấy tóm tắt tồn kho dashboard thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetManagerRecentOrders([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetManagerDashboardRecentOrdersQuery());
        return TypedResults.Ok(BaseResponseModel<ManagerDashboardRecentOrdersDTO>.OkResponseModel(
            data: result,
            message: "Lấy danh sách đơn hàng gần đây thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetStaffSummary([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetStaffDashboardSummaryQuery());
        return TypedResults.Ok(BaseResponseModel<StaffDashboardSummaryDTO>.OkResponseModel(
            data: result,
            message: "Lấy tóm tắt dashboard nhân viên thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetStaffWorkQueue([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetStaffDashboardWorkQueueQuery());
        return TypedResults.Ok(BaseResponseModel<StaffDashboardWorkQueueDTO>.OkResponseModel(
            data: result,
            message: "Lấy hàng đợi công việc dashboard thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetStaffRecentWork([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetStaffDashboardRecentWorkQuery());
        return TypedResults.Ok(BaseResponseModel<StaffDashboardRecentWorkDTO>.OkResponseModel(
            data: result,
            message: "Lấy công việc gần đây thành công.",
            code: ResponseCodeConstants.SUCCESS));
    }
}
