using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.Materials.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class MaterialEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/material")
                       .WithTags("Material")
                       .WithOpenApi();

        group.MapPost("/add", Add);
        group.MapGet("/all", GetAll);
        group.MapGet("/detail/{id}", GetByID);
        group.MapPut("/update/{id}", Update);
    }

    public async Task<IResult> Add(
        [FromServices] ISender sender,
        [FromBody] CreateMaterialCommand command)
    {
        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
            data: result,
            message: "Tạo chất liệu thành công!",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetAll(
        [FromServices] ISender sender)
    {
        var result = await sender.Send(new GetMaterialListQuery());

        return TypedResults.Ok(BaseResponseModel<IEnumerable<MaterialDTO>>.OkResponseModel(
            data: result,
            message: "Lấy danh sách chất liệu thành công!",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> GetByID(
        [FromServices] ISender sender,
        [FromRoute] Guid id)
    {
        var result = await sender.Send(new GetMaterialDetailQuery { Id = id });

        return TypedResults.Ok(BaseResponseModel<MaterialDTO>.OkResponseModel(
            data: result,
            message: "Lấy thông tin chất liệu thành công!",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> Update(
        [FromServices] ISender sender,
        //[FromRoute] Guid id,
        [FromBody] UpdateMateialCommand command)
    {
        //command.Id = id;

        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
            data: result,
            message: "Cập nhật chất liệu thành công!",
            code: ResponseCodeConstants.SUCCESS));
    }
}
