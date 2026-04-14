using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.InventoryTransactions.Commands;
using sp26se058_3dprintshop_be.Application.InventoryTransactions.Queries;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class InventoryTransactionEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory-transaction")
                       .WithTags("Inventory Transaction")
                       .WithOpenApi();

        group.MapPost("/query", QueryTransactions)
                .WithSummary("[Staff/Manager] Lấy danh sách lịch sử biến động kho (Paging, Filter).");

        group.MapGet("/reference/{orderId}", GetByReference)
                .WithSummary("[Staff/Manager] Truy vết lịch sử kho theo ReferenceId (ví dụ OrderId).");

        group.MapPost("/create", CreateTransaction)
                .WithSummary("[Staff/Manager] Tạo mới một giao dịch kho (Nhập/Xuất/Điều chỉnh).");
    }

    public async Task<IResult> QueryTransactions([FromServices] ISender sender, [FromBody] GetInventoryTransactionsWithPaginationQuery query)
    {

        var result = await sender.Send(query);

        //return TypedResults.Ok(BaseResponseModel<IEnumerable<InventoryTransactionDTO>>.OkResponseModel(
        //        code: ResponseCodeConstants.SUCCESS,
        //        data: result.Items,
        //        additionalData: new { paging = result.Metadata },
        //        message: "Lấy danh sách biến động kho thành công"
        //    ));
        return TypedResults.Ok(
            BaseResponseModel<IEnumerable<InventoryTransactionDTO>>
                .ListResponseModel(data: result.Items, additionalData: new { paging = result.Metadata })
                );
    }

    public async Task<IResult> GetByReference([FromServices] ISender sender, [FromRoute] Guid orderId)
    {

        var result = await sender.Send(new GetInventoryTransactionsByReferenceQuery { ReferenceId = orderId });

        //return TypedResults.Ok(BaseResponseModel<List<InventoryTransactionDTO>>.OkResponseModel(
        //        code: ResponseCodeConstants.SUCCESS,
        //        data: result,
        //        message: "Truy vết dữ liệu thành công"
        //    ));
        return TypedResults.Ok(
            BaseResponseModel<IEnumerable<InventoryTransactionDTO>>
                .ListResponseModel(data: result, successMessage: "Truy vết dữ liệu thành công")
                );
    }

    public async Task<IResult> CreateTransaction([FromServices] ISender sender, [FromBody] CreateInventoryTransactionCommand command)
    {

        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<CreateInventoryTransactionCommand>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: result,
                message: "Thực hiện giao dịch kho thành công!"
            ));

    }
}
