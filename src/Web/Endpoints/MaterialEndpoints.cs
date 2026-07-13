using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.Materials.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class MaterialEndpoints : EndpointGroupBase
{
    private const string MaterialWriteDescription = """
        Quản lý vật liệu dùng cho catalog variant, technical draft và tính giá in.

        Rule chính:
        - Material active phải có đúng ít nhất một giá hiện hành IsCurrent=true.
        - Cập nhật thông tin vật liệu không cập nhật giá.
        - Cập nhật giá phải dùng endpoint /update-price để hệ thống tạo MaterialPriceHistory mới và hạ giá cũ khỏi IsCurrent.
        - Deactive chỉ tạm ngưng vật liệu, không soft delete.
        - Delete là xóa mềm và bị chặn nếu vật liệu đã được dùng trong technical draft hoặc biến thể đang mở bán.
        """;

    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/material")
                       .WithTags("Material")
                       .WithOpenApi();

        group.MapGet("/active", GetActive)
            .WithSummary("[All] Lấy danh sách vật liệu đang hoạt động")
            .WithDescription("Dùng cho dropdown chọn vật liệu. Chỉ trả vật liệu IsActive=true và có giá hiện hành.");

        group.MapPost("/query", Query)
            .WithSummary("[Staff/Manager] Truy vấn vật liệu có paging/filter")
            .WithDescription("Hỗ trợ search, lọc IsActive, lọc có giá hiện hành, sort theo name/created/price/effectiveDate.");

        group.MapPost("/add", Add)
            .WithSummary("[Manager] Tạo mới vật liệu")
            .WithDescription(MaterialWriteDescription);

        group.MapGet("/all", GetAll)
            .WithSummary("[Staff/Manager] Lấy toàn bộ danh sách vật liệu")
            .WithDescription("API tương thích cũ. Khuyến nghị FE dùng POST /api/material/query cho paging/filter.");

        group.MapGet("/{id}/detail", GetByID)
            .WithSummary("[Staff/Manager] Xem chi tiết vật liệu")
            .WithDescription("Trả thông tin vật liệu kèm lịch sử giá.");

        group.MapGet("/{id}/price-history", GetPriceHistory)
            .WithSummary("[Staff/Manager] Lấy lịch sử giá vật liệu")
            .WithDescription("Hỗ trợ paging và lọc IsCurrent qua query string.");

        group.MapPut("/{id}/update", Update)
            .WithSummary("[Manager] Cập nhật thông tin vật liệu")
            .WithDescription("Chỉ cập nhật Name, Description, IsActive. Không cập nhật giá; muốn cập nhật giá dùng /update-price.");

        group.MapPatch("/{id}/active", Active)
            .WithSummary("[Manager] Kích hoạt vật liệu")
            .WithDescription("Chỉ kích hoạt được khi vật liệu đã có giá hiện hành.");

        group.MapPatch("/{id}/deactive", Deactive)
            .WithSummary("[Manager] Tạm ngưng vật liệu")
            .WithDescription("Không soft delete. Bị chặn nếu vật liệu đang được dùng bởi biến thể sản phẩm đang mở bán.");

        group.MapDelete("/{id}/delete", Delete)
            .WithSummary("[Manager] Xóa mềm vật liệu")
            .WithDescription("Xóa mềm vật liệu. Bị chặn nếu đã dùng trong technical draft hoặc biến thể đang mở bán.");

        group.MapDelete("/{id}/toggle-active", ToggleActive)
            .WithSummary("[Deprecated][Manager] Chuyển đổi trạng thái active của vật liệu")
            .WithDescription("API tương thích cũ. Chỉ đổi IsActive, không set Deleted. Khuyến nghị dùng /active hoặc /deactive.");

        group.MapPost("/{id}/update-price", UpdatePrice)
            .WithSummary("[Manager] Cập nhật giá của vật liệu")
            .WithDescription("Tạo một MaterialPriceHistory mới IsCurrent=true, đồng thời hạ các giá hiện hành cũ về IsCurrent=false.");

        group.MapGet("/{id}/estimate-cost", EstimateCost)
            .WithSummary("[Staff/Manager] Công cụ tính nhanh giá vật liệu")
            .WithDescription("Nhập ID vật liệu trên route và truyền khối lượng ?weightInGrams=... qua query string.");
    }

    public async Task<IResult> GetActive(
        [FromServices] ISender sender,
        [FromQuery] string? search)
    {
        var result = await sender.Send(new GetActiveMaterialsQuery { Search = search });

        return TypedResults.Ok(BaseResponseModel<IEnumerable<ActiveMaterialDTO>>.ListResponseModel(
            data: result,
            successMessage: "Lấy danh sách vật liệu đang hoạt động thành công.",
            emptyMessage: "Không tìm thấy vật liệu đang hoạt động."));
    }

    public async Task<IResult> Query(
        [FromServices] ISender sender,
        [FromBody] GetMaterialsWithPaginationQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(BaseResponseModel<IEnumerable<MaterialDTO>>.ListResponseModel(
            data: result.Items,
            additionalData: new { paging = result.Metadata },
            successMessage: "Lấy danh sách vật liệu thành công.",
            emptyMessage: "Không tìm thấy vật liệu phù hợp."));
    }

    public async Task<IResult> Add(
        [FromServices] ISender sender,
        [FromBody] CreateMaterialCommand command)
    {
        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<MaterialDTO>.OkResponseModel(
            data: result,
            message: "Tạo vật liệu thành công!",
            code: ResponseCodeConstants.CREATED));
    }

    public async Task<IResult> GetAll(
        [FromServices] ISender sender)
    {
        var result = await sender.Send(new GetMaterialListQuery());

        return TypedResults.Ok(BaseResponseModel<IEnumerable<MaterialDTO>>.ListResponseModel(
            data: result,
            successMessage: "Lấy danh sách vật liệu thành công!",
            emptyMessage: "Không tìm thấy vật liệu nào."));
    }

    public async Task<IResult> GetByID(
        [FromServices] ISender sender,
        [FromRoute] Guid id)
    {
        var result = await sender.Send(new GetMaterialDetailQuery { Id = id });

        return TypedResults.Ok(BaseResponseModel<MaterialDTO>.OkResponseModel(
            data: result,
            message: "Lấy thông tin vật liệu thành công!",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetPriceHistory(
        [FromServices] ISender sender,
        [FromRoute] Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isCurrent = null)
    {
        var result = await sender.Send(new GetMaterialPriceHistoryQuery
        {
            MaterialId = id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            IsCurrent = isCurrent
        });

        return TypedResults.Ok(BaseResponseModel<IEnumerable<MaterialPriceHistoryDTO>>.ListResponseModel(
            data: result.Items,
            additionalData: new { paging = result.Metadata },
            successMessage: "Lấy lịch sử giá vật liệu thành công.",
            emptyMessage: "Không tìm thấy lịch sử giá vật liệu."));
    }

    public async Task<IResult> Update(
        [FromServices] ISender sender,
        [FromRoute] Guid id,
        [FromBody] UpdateMaterialCommand command)
    {
        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);

        return TypedResults.Ok(BaseResponseModel<MaterialDTO>.OkResponseModel(
            data: result,
            message: "Cập nhật vật liệu thành công!",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> Active(
        [FromServices] ISender sender,
        [FromRoute] Guid id)
    {
        var result = await sender.Send(new ActiveMaterialCommand(id));

        return TypedResults.Ok(BaseResponseModel<MaterialStatusResult>.OkResponseModel(
            data: result,
            message: "Đã kích hoạt vật liệu.",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> Deactive(
        [FromServices] ISender sender,
        [FromRoute] Guid id)
    {
        var result = await sender.Send(new DeactiveMaterialCommand(id));

        return TypedResults.Ok(BaseResponseModel<MaterialStatusResult>.OkResponseModel(
            data: result,
            message: "Đã tạm ngưng vật liệu.",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> Delete(
        [FromServices] ISender sender,
        [FromRoute] Guid id)
    {
        var result = await sender.Send(new DeleteMaterialCommand { Id = id });

        return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
            data: result,
            message: "Xóa mềm vật liệu thành công!",
            code: ResponseCodeConstants.DELETED));
    }

    public async Task<IResult> ToggleActive(
        [FromServices] ISender sender,
        [FromRoute] Guid id)
    {
        var result = await sender.Send(new ToggleMaterialActiveCommand { Id = id });

        return TypedResults.Ok(BaseResponseModel<MaterialStatusResult>.OkResponseModel(
            data: result,
            message: "Cập nhật trạng thái hoạt động của vật liệu thành công!",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> UpdatePrice(
        [FromServices] ISender sender,
        [FromRoute] Guid id,
        [FromBody] UpdateMaterialPriceCommand command)
    {
        var finalCommand = command with { MaterialId = id };
        var result = await sender.Send(finalCommand);

        return TypedResults.Ok(BaseResponseModel<MaterialDTO>.OkResponseModel(
            data: result,
            message: "Cập nhật giá vật liệu thành công!",
            code: ResponseCodeConstants.UPDATED));
    }

    public async Task<IResult> EstimateCost(
        [FromServices] ISender sender,
        [FromRoute] Guid id,
        [FromQuery] double weightInGrams)
    {
        var command = new CalculateMaterialPriceCommand { MaterialId = id, WeightInGrams = weightInGrams };
        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            data: result,
            message: "Tính toán chi phí vật liệu ước tính thành công!",
            code: ResponseCodeConstants.SUCCESS));
    }
}
