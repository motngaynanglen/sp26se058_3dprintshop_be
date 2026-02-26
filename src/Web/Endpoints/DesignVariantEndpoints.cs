using Microsoft.AspNetCore.Mvc;
using Mysqlx.Crud;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;
using sp26se058_3dprintshop_be.Application.DesignVariant.Commands;
using sp26se058_3dprintshop_be.Application.DesignVariant.Queries;
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
        group.MapGet("/all", GetAll);
        group.MapPost("/add", Add);
        group.MapPut("/update/{id}", Update);
        //group.MapDelete("/delete/{id}", Delete);

        /*
        Của customer
        group.MapGet("", GetByTemplateID)
        group.MapGet("/detail/{id}", GetByID)
        group.MapPatch("/price", UpdatePrice)
        group.MapPatch("/stock-quantity", UpdateStockQuantity)
         */



    }

    public async Task<IResult> Add([FromServices] ISender sender, [FromBody] CreateDesignVariantCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                    data: result,
                    message: "Tạo biến thể thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch(Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                    data: ex.Message,
                    message: "Tạo biến thể thất bại!"
                ));
        }
    }

    public async Task<IResult> Update([FromServices] ISender sender,[FromBody] UpdateDesignVariantCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                    data: result,
                    message: "Cập nhật biến thể thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                    data: ex.Message,
                    message: "Cập nhật biến thể thất bại!"
                ));
        }
    }

    public async Task<IResult> GetAll( [FromServices] ISender sender, [FromBody] GetDesignVariantListQuery query)
    {
        try
        {
            var result = await sender.Send(query);
            return TypedResults.Ok(BaseResponseModel<IEnumerable<DesignVariantDTO>>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result.Items,
                    additionalData: new { pagination = result.Metadata },
                    message: "Truy vấn biến thể thành công!"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                    data: ex.Message,
                    message: "Truy vấn biến thể thất bại!"
                ));
        }
    }
}
