using Microsoft.AspNetCore.Mvc;
using Mysqlx.Crud;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.DesignVariants.Commands;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class DesignVariantEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/design-variant")
                       .WithTags("Design Variant")
                       .WithOpenApi();
        group.MapPost("/query", GetAll)
            .WithSummary("[All] Truy vấn danh sách Biến thể")
            .WithDescription("Hỗ trợ lọc theo TemplateId, MaterialId. Tự động ẩn hàng IsActive=false đối với Customer.");

        group.MapPost("/add", Add)
            .WithSummary("[Staff/Manager] Thêm mới biến thể")
            .WithDescription("Tạo biến thể mới cho mẫu thiết kế. Tự động kiểm tra trùng Code và sự tồn tại của Material.");

        group.MapPatch("/{id}/update", Update)
            .WithSummary("[Staff/Manager] Cập nhật thông tin biến thể")
            .WithDescription("Cập nhật từng phần (Partial Update). ID từ URL sẽ ghi đè ID trong body.");

        //group.MapPatch("/{id}/quantity", UpdateQuantity)
        //    .WithSummary("Điều chỉnh số lượng kho")
        //    .WithDescription("Cập nhật số lượng StockQuantity trong kho cho biến thể.");

        group.MapDelete("/{id}/delete", Delete)
            .WithSummary("[Staff/Manager] Xoá biến thể (Soft Delete)")
            .WithDescription("Chuyển trạng thái xóa mềm. Ngăn xóa nếu đã có OrderItems.");

    }



    public async Task<IResult> Add([FromServices] ISender sender, [FromBody] CreateDesignVariantCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Ok(BaseResponseModel<DesignVariantDTO>.OkResponseModel(
                data: result,
                message: "Tạo biến thể mới thành công!",
                code: ResponseCodeConstants.CREATED
            ));
    }

    public async Task<IResult> Update([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateDesignVariantCommand command)
    {
        // Đồng nhất ID từ Route vào Command
        var finalCmd = command with { Id = id };
        var result = await sender.Send(finalCmd);
        return TypedResults.Ok(BaseResponseModel<DesignVariantDTO>.OkResponseModel(
                data: result,
                message: "Cập nhật biến thể thành công!",
                code: ResponseCodeConstants.UPDATED
            ));
    }

    //public async Task<IResult> UpdateQuantity([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateDesignVariantQuantityCommand command)
    //{
    //    // Đồng nhất ID từ Route
    //    var finalCmd = command with { Id = id };
    //    var result = await sender.Send(finalCmd);
    //    return TypedResults.Ok(BaseResponseModel<DesignVariantDTO>.OkResponseModel(
    //            data: result,
    //            message: "Cập nhật số lượng kho thành công!",
    //            code: ResponseCodeConstants.SUCCESS
    //        ));
    //}

    public async Task<IResult> GetAll([FromServices] ISender sender, [FromBody] GetDesignVariantListQuery query)
    {
        var result = await sender.Send(query);
        bool isEmpty = result.Any();

        return TypedResults.Ok(BaseResponseModel<List<DesignVariantDTO>>.OkResponseModel(
                data: result,
                message: isEmpty ? "Lấy danh sách biến thể của mẫu thiết kế thành công!" : "Không tìm thấy kết quả nào phù hợp.",
                code: isEmpty ? ResponseCodeConstants.SUCCESS : ResponseCodeConstants.EMPTY_RESULT
            ));
    }

    public async Task<IResult> Delete([FromServices] ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new DeleteDesignVariantCommand { Id = id });
        return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
                data: result,
                message: "Xoá biến thể thành công!",
                code: ResponseCodeConstants.SUCCESS
            ));
    }
}
