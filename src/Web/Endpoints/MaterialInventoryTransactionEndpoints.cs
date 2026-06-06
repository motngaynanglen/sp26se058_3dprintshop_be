using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.MaterialInventoryTransactions.Commands;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class MaterialInventoryTransactionEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/material-inventory-transaction")
            .WithTags("Material Inventory Transaction")
            .WithOpenApi();

        group.MapPost("/create", CreateTransaction)
            .WithSummary("[Staff/Manager] Nhập/xuất/điều chỉnh kho vật liệu (gram).");
    }

    public async Task<IResult> CreateTransaction(
        [FromServices] ISender sender,
        [FromBody] CreateMaterialInventoryTransactionCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<CreateMaterialInventoryTransactionCommand>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Thực hiện giao dịch kho vật liệu thành công!"));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
