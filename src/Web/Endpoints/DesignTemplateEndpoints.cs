
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.DesignTags.Queries;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class DesignTemplateEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/design-template")
                       .WithTags("Design Template")
                       .WithOpenApi();

        // --- CÁC ENDPOINT TRUY VẤN (READ) ---

        group.MapPost("/query", Query)
            .WithSummary("[All] Truy vấn danh sách mẫu thiết kế có phân trang")
            .WithDescription("Hệ thống tự động chỉ hiển thị dữ liệu đang hoạt động đối với khách hàng. Nhân viên/quản lý có thể xem toàn bộ.");

        group.MapGet("{id}/detail", GetDetail)
            .WithSummary("[All] Xem chi tiết một mẫu thiết kế")
            .WithDescription("Trả về thông tin chi tiết cùng các thuộc tính cơ bản của mẫu thiết kế.");

        group.MapGet("/{id}/tags", GetTags)
            .WithSummary("[All] Lấy danh sách các Tag của mẫu thiết kế")
            .WithDescription("Trả về danh sách các nhãn (tags) được gắn cho mẫu thiết kế này.");


        // --- CÁC ENDPOINT THAY ĐỔI DỮ LIỆU (WRITE) ---

        group.MapPost("/add", Create)
            .WithSummary("[Staff/Manager] Tạo mới mẫu thiết kế")
            .WithDescription("Chỉ dành cho nhân viên hoặc quản lý. Yêu cầu nhập đầy đủ mã và tên.");

        group.MapPatch("/{id}/update", Update)
            .WithSummary("[Staff/Manager] Cập nhật thông tin mẫu thiết kế")
            .WithDescription("Cập nhật từng phần (Partial Update). Ghi đè ID từ URL vào Command.");

        group.MapDelete("/{id}/delete", Delete)
            .WithSummary("[Staff/Manager] Xóa mềm mẫu thiết kế")
            .WithDescription("Chuyển trạng thái sang Deleted. Lưu vết người xóa.");
    }

    public async Task<IResult> GetTags([FromServices] ISender sender, [FromRoute] Guid id)
    {

        var result = await sender.Send(new GetDesignTagsListQuery
        {
            DesignTemplateId = id
        });
        return TypedResults.Ok(BaseResponseModel<IEnumerable<DesignTagDTO>>.ListResponseModel(
                data: result,
                successMessage: "Lấy tags mẫu thiết kế thành công!",
                emptyMessage: "Không tìm thấy tag nào cho mẫu thiết kế này."
            ));

    }
    /// <summary>
    /// Lấy danh sách phân trang mẫu thiết kế.
    /// Nếu danh sách trống, trả về mảng rỗng [] và 200 OK theo logic Search.
    /// </summary>
    public async Task<IResult> Query([FromServices] ISender sender, [FromBody] GetDesignTemplatesWithPaginationQuery query)
    {

        var result = await sender.Send(query);
        //return TypedResults.Ok(BaseResponseModel<IEnumerable<DesignTemplateDTO>>.OkResponseModel(
        //        code: result.Items.Any() ? ResponseCodeConstants.SUCCESS : ResponseCodeConstants.EMPTY_RESULT,
        //        data: result.Items,
        //        additionalData: new { pagination = result.Metadata },
        //        message: result.Items.Any() ? "Lấy danh sách thành công" : "Không tìm thấy kết quả nào phù hợp."
        //    ));
        return TypedResults.Ok(
                BaseResponseModel<IEnumerable<DesignTemplateDTO>>
                    .ListResponseModel(data: result.Items, additionalData: new { paging = result.Metadata })
                );


    }

    public async Task<IResult> Create([FromServices] ISender sender, [FromBody] CreateDesignTemplateCommand command)
    {

        var result = await sender.Send(command);
        return TypedResults.Ok(BaseResponseModel<DesignTemplateDTO>.OkResponseModel(
                data: result,
                message: "Tạo mẫu thiết kế thành công!",
                code: ResponseCodeConstants.CREATED
            ));


    }

    public async Task<IResult> GetDetail([FromServices] ISender sender, [FromRoute] Guid id)
    {

        var result = await sender.Send(new GetDesignTemplateDetailQuery
        {
            Id = id
        });
        return TypedResults.Ok(BaseResponseModel<DesignTemplateDTO>.OkResponseModel(
                data: result,
                message: "Lấy chi tiết mẫu thiết kế thành công!",
                code: ResponseCodeConstants.SUCCESS
            ));

    }

    public async Task<IResult> Update([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateDesignTemplateCommand command)
    {

        var finalCmd = command with { Id = id };
        var result = await sender.Send(finalCmd);
        return TypedResults.Ok(BaseResponseModel<DesignTemplateDTO>.OkResponseModel(
                data: result,
                message: "Cập nhật mẫu thiết kế thành công!",
                code: ResponseCodeConstants.UPDATED
            ));

    }

    public async Task<IResult> Delete([FromServices] ISender sender, [FromRoute] Guid id)
    {

        var result = await sender.Send(new DeleteDesignTemplateCommand { Id = id });
        return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
                data: result,
                message: "Xoá mềm mẫu thiết kế thành công!",
                code: ResponseCodeConstants.DELETED
            ));

    }
}
