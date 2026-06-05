
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;
using sp26se058_3dprintshop_be.Application.DesignWorks.Commands;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class DesignWorkEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/design-work")
                       .WithTags("Design Work")
                       .WithOpenApi();

        group.MapPost("/query", Query)
            .WithSummary("[All] Truy vấn danh sách công việc thiết kế có phân trang")
            .WithDescription("Hệ thống tự động chỉ hiển thị dữ liệu đang hoạt động đối với khách hàng. Nhân viên/quản lý có thể xem toàn bộ.");
        group.MapGet("/{id}/detail", GetDetail)
            .WithSummary("[All] Xem chi tiết một công việc thiết kế")
            .WithDescription("Trả về thông tin chi tiết, dịch vụ đã chọn và đơn phí thiết kế liên quan.");
        // Gắn API Quick Print cho khách hàng
        group.MapPost("/quick-print", CreateQuickPrint)
            .WithSummary("[Customer] Gửi yêu cầu in 3D từ file có sẵn")
            .WithDescription("Khách hàng upload tối đa 5 file (.glb/.stl/.obj) và gửi yêu cầu để nhân viên kiểm tra kỹ thuật và báo giá. " +
                             "Tạo mới DesignWork loại PRINT_SERVICE ở trạng thái REVIEWING.");
        group.MapPost("/add", Create)
            .WithSummary("[Staff/Manager] Tạo mới công việc thiết kế")
            .WithDescription("Chỉ dành cho nhân viên hoặc quản lý. Yêu cầu nhập đầy đủ mã và tên.");
        group.MapPatch("/{id}/update", Update)
            .WithSummary("[Staff/Manager] Cập nhật thông tin công việc thiết kế")
            .WithDescription("Cập nhật từng phần (Partial Update). Ghi đè ID nếu có.");
        group.MapPatch("/{id}/mark-approve", UpdateIsApprove)
            .WithSummary("[Staff/Manager] Cập nhật trạng thái phê duyệt công việc thiết kế")
            .WithDescription("Chỉ dành cho nhân viên hoặc quản lý. Cập nhật trạng thái phê duyệt của công việc thiết kế.");
        group.MapPatch("/{id}/mark-printable", UpdateIsPrintable)
            .WithSummary("[Staff/Manager] [DEPRECATED] Cập nhật trạng thái có thể in")
            .WithDescription("⚠️ DEPRECATED — Dùng POST /{id}/review-file thay thế. Endpoint này sẽ bị xóa trong phiên bản kế tiếp.");

        group.MapPost("/{versionHistoryId}/review-file", ReviewFileVersion)
            .WithSummary("[Staff/Manager] Duyệt file kỹ thuật (dùng chung cho cả Design Service và Quick Print)")
            .WithDescription("Nhân viên xác nhận hoặc từ chối một file DesignVersionHistory dựa trên tiêu chuẩn kỹ thuật in. " +
                             "Khi từ chối: phải nhập lý do (ReviewNote). " +
                             "Quick Print: nếu tất cả file bị từ chối, DesignWork chuyển về SKETCHING để khách upload lại.");
        group.MapPatch("/{id}/lock", Lock)
            .WithSummary("[Customer/Staff/Manager] Khóa công việc thiết kế")
            .WithDescription("Khóa DesignWork để bảo toàn lịch sử sau khi đã chốt file hoặc kết thúc hỗ trợ.");
        group.MapPost("/{id}/request-rework", RequestRework)
            .WithSummary("[Customer] Yêu cầu chỉnh sửa thêm")
            .WithDescription("Tạo một phiên bản chỉnh sửa mới từ công việc thiết kế đã có, giữ quan hệ cha/gốc để tra lịch sử.");

        group.MapPost("/{id}/add-files", AddFilesToQuickPrint)
            .WithSummary("[Customer] Re-upload file vào yêu cầu in nhanh")
            .WithDescription("Khách hàng upload lại file sau khi tất cả file bị nhân viên từ chối (trạng thái SKETCHING). " +
                             "Chỉ áp dụng cho yêu cầu in nhanh (PRINT_SERVICE). Tự động chuyển trạng thái về REVIEWING.");

        group.MapDelete("/{id}/close", CloseQuickPrintRequest)
            .WithSummary("[Customer/Staff/Manager] Đóng yêu cầu in nhanh")
            .WithDescription("Khóa (đóng) một yêu cầu in nhanh CHƯA có đơn hàng. " +
                             "Yêu cầu vẫn hiển thị trong lịch sử (IsLocked = true), nhưng không thể upload thêm file hoặc tạo draft. " +
                             "Khách hàng chỉ đóng được yêu cầu của mình. Nhân viên/quản lý đóng được mọi yêu cầu. " +
                             "Nếu đã có đơn hàng, dùng API hủy đơn hàng.");

    }

    public async Task<IResult> Query([FromServices] ISender sender, [FromBody] GetPaginationDesignWorkQuery request)
    {
        var result = await sender.Send(request);
        return TypedResults.Ok(BaseResponseModel<IEnumerable<DesignWorkDTO>>.ListResponseModel(
                data: result.Items,
                additionalData: new { pagination = result.Metadata },
                successMessage: "Lấy danh sách công việc thiết kế thành công.",
                emptyMessage: "Không tìm thấy công việc thiết kế nào phù hợp."
            ));
    }

    public async Task<IResult> GetDetail([FromServices] ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new GetDesignWorkDetailQuerry
        {
            Id = id
        });
        return TypedResults.Ok(BaseResponseModel<DesignWorkDetailDTO>.OkResponseModel(
                data: result,
                message: "Lấy chi tiết công việc thiết kế thành công!",
                code: ResponseCodeConstants.SUCCESS
            ));
    }

    public async Task<IResult> Create([FromServices] ISender sender, [FromBody] CreateDesignWorkCommand request)
    {
        var result = await sender.Send(request);
        return TypedResults.Ok(BaseResponseModel<DesignWorkDTO>.OkResponseModel(
                data: result,
                message: "Tạo công việc thiết kế thành công!",
                code: ResponseCodeConstants.CREATED
            ));
    }

    public async Task<IResult> Update([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateDesignWorkCommand command)
    {
        var finalCmd = command with { Id = id };
        var result = await sender.Send(finalCmd);
        return TypedResults.Ok(BaseResponseModel<DesignWorkDTO>.OkResponseModel(
                data: result,
                message: "Cập nhật công việc thiết kế thành công!",
                code: ResponseCodeConstants.UPDATED
            ));
    }

    public async Task<IResult> UpdateIsApprove([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateDesignWorkIsApproveCommand command)
    {
        var finalCmd = command with { Id = id };
        var result = await sender.Send(finalCmd);
        return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
                data: result,
                message: "Cập nhật trạng thái phê duyệt công việc thiết kế thành công!",
                code: ResponseCodeConstants.UPDATED
            ));
    }

    public async Task<IResult> UpdateIsPrintable([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateDesignVersionHistoryIsPrintableCommand command)
    {
        var finalCmd = command with { Id = id };
        var result = await sender.Send(finalCmd);
        return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
                data: result,
                message: "Cập nhật trạng thái có thể in công việc thiết kế thành công!",
                code: ResponseCodeConstants.UPDATED
         ));
    }

    public async Task<IResult> Lock([FromServices] ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new LockDesignWorkCommand { Id = id });
        return TypedResults.Ok(BaseResponseModel<DesignWorkDTO>.OkResponseModel(
                data: result,
                message: "Khóa công việc thiết kế thành công!",
                code: ResponseCodeConstants.UPDATED
            ));
    }

    public async Task<IResult> RequestRework([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] RequestDesignWorkReworkCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return TypedResults.Ok(BaseResponseModel<DesignWorkDTO>.OkResponseModel(
                data: result,
                message: "Tạo yêu cầu chỉnh sửa thành công!",
                code: ResponseCodeConstants.CREATED
            ));
    }

    //public async Task<IResult> Delete([FromServices] ISender sender, [FromRoute] Guid id)
    //{
    //   var result = await sender.Send(new DeleteDesignWorkCommand { Id = id });
    //   return TypedResults.Ok(BaseResponseModel<string>.OkResponseModel(
    //           data: result,
    //           message: "Xóa công việc thiết kế thành công!",
    //           code: ResponseCodeConstants.DELETED
    //       ));
    //}
    public async Task<IResult> ReviewFileVersion(
        [FromServices] ISender sender,
        [FromRoute] Guid versionHistoryId,
        [FromBody] ReviewFileVersionCommand command)
    {
        var finalCmd = command with { VersionHistoryId = versionHistoryId };
        var result = await sender.Send(finalCmd);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                data: result,
                message: "Duyệt file thành công!",
                code: ResponseCodeConstants.UPDATED
            ));
    }

    public async Task<IResult> CreateQuickPrint([FromServices] ISender sender, [FromBody] AddFilesToQuickPrintCommand request)
    {
        // DesignWorkId is null (not in body) — handler creates a new Quick Print DesignWork
        var result = await sender.Send(request);
        return TypedResults.Ok(BaseResponseModel<DesignWorkDTO>.OkResponseModel(
                data: result,
                message: "Gửi yêu cầu in thành công! Vui lòng chờ nhân viên kỹ thuật kiểm tra file và báo giá.",
                code: ResponseCodeConstants.CREATED
            ));
    }

    public async Task<IResult> AddFilesToQuickPrint(
        [FromServices] ISender sender,
        [FromRoute] Guid id,
        [FromBody] AddFilesToQuickPrintCommand command)
    {
        // DesignWorkId injected from route — handler re-uploads files to existing SKETCHING work
        var finalCmd = command with { DesignWorkId = id };
        var result = await sender.Send(finalCmd);
        return TypedResults.Ok(BaseResponseModel<DesignWorkDTO>.OkResponseModel(
                data: result,
                message: "Upload file thành công! Yêu cầu in đã được gửi lại cho nhân viên kiểm tra.",
                code: ResponseCodeConstants.CREATED
            ));
    }

    public async Task<IResult> CloseQuickPrintRequest(
        [FromServices] ISender sender,
        [FromRoute] Guid id,
        [FromBody] CloseQuickPrintRequestCommand command)
    {
        var finalCmd = command with { DesignWorkId = id };
        var result = await sender.Send(finalCmd);
        return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
                data: result,
                message: "Yêu cầu in đã được đóng thành công.",
                code: ResponseCodeConstants.UPDATED
            ));
    }
}
