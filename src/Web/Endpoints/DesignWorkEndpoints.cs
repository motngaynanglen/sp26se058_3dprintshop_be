
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
            .WithDescription("Hệ thống tự động lọc IsActive = true đối với khách hàng. Staff/Manager có thể xem toàn bộ.");
        //group.MapGet("{id}/detail", GetDetail)
        //    .WithSummary("[All] Xem chi tiết một công việc thiết kế")
        //    .WithDescription("Trả về thông tin chi tiết cùng các thuộc tính cơ bản của công việc thiết kế.");
        // Gắn API Quick Print cho khách hàng
        group.MapPost("/quick-print", CreateQuickPrint)
            .WithSummary("[Customer] Gửi yêu cầu in 3D từ file có sẵn")
            .WithDescription("Khách hàng upload tối đa 5 file và gửi yêu cầu để nhân viên báo giá.");
        group.MapPost("/add", Create)
            .WithSummary("[Staff/Manager] Tạo mới công việc thiết kế")
            .WithDescription("Chỉ dành cho Staff hoặc Manager. Yêu cầu nhập đầy đủ Code và Name.");
        group.MapPatch("/{id}/update", Update)
            .WithSummary("[Staff/Manager] Cập nhật thông tin công việc thiết kế")
            .WithDescription("Cập nhật từng phần (Partial Update). Ghi đè ID nếu có.");
        group.MapPatch("/{id}/mark-approve", UpdateIsApprove)
            .WithSummary("[Staff/Manager] Cập nhật trạng thái phê duyệt công việc thiết kế")
            .WithDescription("Chỉ dành cho Staff hoặc Manager. Cập nhật trạng thái phê duyệt của công việc thiết kế.");
        group.MapPatch("/{id}/mark-printable", UpdateIsPrintable)
            .WithSummary("[Staff/Manager] Cập nhật trạng thái có thể in công việc thiết kế")
            .WithDescription("Chỉ dành cho Staff hoặc Manager. Cập nhật trạng thái có thể in của công việc thiết kế.");
        group.MapPatch("/{id}/lock", Lock)
            .WithSummary("[Customer/Staff/Manager] Khóa công việc thiết kế")
            .WithDescription("Khóa DesignWork để bảo toàn lịch sử sau khi đã chốt file hoặc kết thúc hỗ trợ.");
        group.MapPost("/{id}/request-rework", RequestRework)
            .WithSummary("[Customer] Yêu cầu chỉnh sửa thêm")
            .WithDescription("Tạo một DesignWork revision mới từ DesignWork đã có, giữ quan hệ Parent/Root để tra lịch sử.");

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
        return TypedResults.Ok(BaseResponseModel<DesignWorkDTO>.OkResponseModel(
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
    public async Task<IResult> CreateQuickPrint([FromServices] ISender sender, [FromBody] CheckoutQuickPrintCommand request)
    {
        var result = await sender.Send(request);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                data: result,
                message: "Gửi yêu cầu in thành công! Vui lòng chờ nhân viên kỹ thuật kiểm tra file và báo giá.",
                code: ResponseCodeConstants.CREATED
            ));
    }
}
