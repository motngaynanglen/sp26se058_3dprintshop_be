using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class MaterialEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/material")
                       .WithTags("Material")
                       .WithOpenApi();

        group.MapPost("/add", Add)
            .WithSummary("[Manager] Tạo mới vật liệu")
            .WithDescription("Tạo vật liệu mới với các thông tin: giá, mô tả, ...");
        group.MapGet("/all", GetAll)
            .WithSummary("[Manager] Lấy danh sách vật liệu");
        group.MapGet("/{id}/detail", GetByID)
            .WithSummary("[Manager] Xem chi tiết vật liệu");
        group.MapPut("/{id}/update", Update)
            .WithSummary("[Manager] Cập nhật vật liệu");
        group.MapDelete("/{id}/toggle-active", Delete)
            .WithSummary("Chuyển đổi trạng thái Active của vật liệu");
        group.MapPost("/{id}/update-price", UpdatePrice)
            .WithSummary("[Manager] Cập nhật giá của vật liệu");
    }

    public async Task<IResult> Add(
        [FromServices] ISender sender,
        [FromBody] CreateMaterialCommand command)
    {
        try
        {
            var result = await sender.Send(command);

            return TypedResults.Ok(BaseResponseModel<MaterialDTO>.OkResponseModel(
                data: result,
                message: "Tạo chất liệu thành công!",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    public async Task<IResult> GetAll(
        [FromServices] ISender sender)
    {
        try
        {
            var result = await sender.Send(new GetMaterialListQuery());

            return TypedResults.Ok(BaseResponseModel<IEnumerable<MaterialDTO>>.OkResponseModel(
                data: result,
                message: "Lấy danh sách chất liệu thành công!",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }

    }

    public async Task<IResult> GetByID(
        [FromServices] ISender sender,
        [FromRoute] Guid id)
    {
        try
        {
            var result = await sender.Send(new GetMaterialDetailQuery { Id = id });

            return TypedResults.Ok(BaseResponseModel<MaterialDTO>.OkResponseModel(
                data: result,
                message: "Lấy thông tin chất liệu thành công!",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
        
    }

    public async Task<IResult> Update(
        [FromServices] ISender sender,
        [FromRoute] Guid id,
        [FromBody] UpdateMaterialCommand command)
    {
        
        try
        {
            var finalCommand = command with { Id = id };
            var result = await sender.Send(finalCommand);

            return TypedResults.Ok(BaseResponseModel<MaterialDTO>.OkResponseModel(
                data: result,
                message: "Cập nhật chất liệu thành công!",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    public async Task<IResult> Delete([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] DeleteMaterialCommand command)
    {
        try
        {
            var finalCommand = command with { Id = id };
            var result = await sender.Send(finalCommand);
            return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
                data: result,
                message: "Cập nhật IsActive của chất liệu thành công!",
                code: ResponseCodeConstants.SUCCESS));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Lỗi trong quá trình cập nhật"
            ));
        }
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
            message: "Cập nhật giá chất liệu thành công!",
            code: ResponseCodeConstants.SUCCESS));
    }
}
