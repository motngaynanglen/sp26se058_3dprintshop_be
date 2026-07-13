
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
    private const string CreateDesignTemplateDescription = """
        Tạo một mẫu thiết kế trong catalog.

        Trạng thái catalog hỗ trợ:
        - DRAFT: bản nháp nội bộ. Staff/Manager xem và chỉnh sửa được; Customer/Guest không thấy trong shop và không mua được.
        - PUBLISHED: đã công khai. Customer/Guest có thể thấy trong shop nếu IsActive=true và các biến thể liên quan cũng PUBLISHED/IsActive=true.
        - ARCHIVED: đã lưu trữ/xóa mềm theo nghiệp vụ. Không dùng khi tạo mới; hệ thống dùng khi xóa mềm hoặc ngưng kinh doanh.

        Khuyến nghị khi tạo mới:
        - Nên tạo với catalogStatus=DRAFT để kiểm tra file, ảnh, tag, biến thể và giá trước khi mở bán.
        - Nếu không truyền catalogStatus, hệ thống mặc định theo rule command hiện tại: DRAFT, trừ khi dùng IsAcitve=true thì sẽ được hiểu là PUBLISHED.
        - IsActive được đồng bộ theo catalogStatus: PUBLISHED => true, DRAFT/ARCHIVED => false.

        Cách publish:
        - Gọi PATCH /api/design-template/{id}/update với body: { "catalogStatus": "PUBLISHED" }.
        - Nếu template có biến thể, cần publish từng biến thể bằng PATCH /api/design-variant/{id}/update với catalogStatus=PUBLISHED để khách có thể mua.
        """;

    private const string CreateCatalogProductDescription = """
        Tạo sản phẩm catalog hoàn chỉnh trong một transaction: DesignTemplate + DesignVariant + DesignTag.

        Trạng thái catalog hỗ trợ:
        - DRAFT: bản nháp nội bộ. Phù hợp khi vừa nhập sản phẩm, cần kiểm tra ảnh, model, giá, vật liệu và tag.
        - PUBLISHED: đã công khai. Chỉ nên dùng khi template và ít nhất một variant đã sẵn sàng mở bán.
        - ARCHIVED: đã lưu trữ/xóa mềm; không nên dùng khi tạo mới.

        Rule khi tạo product:
        - Nếu product catalogStatus=PUBLISHED thì phải có ít nhất một variant PUBLISHED.
        - Variant có catalogStatus riêng. Nếu không truyền cho variant, hệ thống dùng status của product.
        - Ảnh sản phẩm nằm ở từng variant qua ImageUrls, vì mỗi variant có thể khác màu/chất liệu/hình ảnh.
        - Tags dùng để phân loại template. Chỉ được có tối đa một tag chính IsMainTag=true; nếu không có tag chính, hệ thống tự lấy tag đầu tiên làm tag chính.

        Cách publish sau khi tạo nháp:
        - Publish template: PATCH /api/design-template/{id}/update với { "catalogStatus": "PUBLISHED" }.
        - Publish variant: PATCH /api/design-variant/{id}/update với { "catalogStatus": "PUBLISHED" }.
        - Khách chỉ thấy/mua được khi cả template và variant đều PUBLISHED và IsActive=true.
        """;

    private const string UpdateDesignTemplateDescription = """
        Cập nhật thông tin mẫu thiết kế theo từng phần.

        Dùng endpoint này để publish/unpublish template:
        - Publish/mở bán template: gửi { "catalogStatus": "PUBLISHED" }.
        - Đưa về bản nháp/nội bộ: gửi { "catalogStatus": "DRAFT" }.
        - Lưu trữ theo nghiệp vụ: ưu tiên dùng DELETE /api/design-template/{id}/delete để hệ thống xử lý soft delete và audit.

        Lưu ý khi publish:
        - Publish template chỉ mở trạng thái của template.
        - Khách vẫn chưa mua được nếu tất cả variant còn DRAFT/ARCHIVED hoặc IsActive=false.
        - Cần publish ít nhất một variant bằng PATCH /api/design-variant/{id}/update với { "catalogStatus": "PUBLISHED" }.

        Tương thích cũ:
        - Có thể dùng IsAcitve=true để chuyển template sang PUBLISHED.
        - Có thể dùng IsAcitve=false để chuyển template sang DRAFT.
        - Ưu tiên dùng catalogStatus vì rõ nghĩa hơn IsAcitve.
        """;

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
            .WithDescription(CreateDesignTemplateDescription);

        group.MapPost("/product/add", CreateCatalogProduct)
            .WithSummary("[Staff/Manager] Tạo sản phẩm catalog hoàn chỉnh")
            .WithDescription(CreateCatalogProductDescription);

        group.MapPatch("/{id}/update", Update)
            .WithSummary("[Staff/Manager] Cập nhật thông tin mẫu thiết kế")
            .WithDescription(UpdateDesignTemplateDescription);

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

    public async Task<IResult> CreateCatalogProduct([FromServices] ISender sender, [FromBody] CreateCatalogProductCommand command)
    {

        var result = await sender.Send(command);
        return TypedResults.Ok(BaseResponseModel<DesignTemplateDTO>.OkResponseModel(
                data: result,
                message: "Tạo sản phẩm catalog thành công!",
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
