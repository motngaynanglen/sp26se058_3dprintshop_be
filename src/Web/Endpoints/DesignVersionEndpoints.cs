using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.DesignVersions.Commands;
using sp26se058_3dprintshop_be.Application.DesignVersions.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class DesignVersionEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/design-version")
            .WithTags("Design Version")
            .WithOpenApi();

        group.MapGet("/design-work/{designWorkId}", GetByDesignWork)
            .WithSummary("[Customer/Staff/Manager] Lấy danh sách file version theo DesignWork.");
        group.MapGet("/{id}/detail", GetDetail)
            .WithSummary("[Customer/Staff/Manager] Lấy chi tiết file version.");
        group.MapPatch("/{id}/printable", UpdatePrintable)
            .WithSummary("[Staff/Manager] Cập nhật trạng thái có thể in của file version.");
    }

    public async Task<IResult> GetByDesignWork([FromServices] ISender sender, [FromRoute] Guid designWorkId)
    {
        var result = await sender.Send(new GetDesignVersionsByWorkQuery(designWorkId));
        return TypedResults.Ok(BaseResponseModel<List<DesignVersionHistoryDTO>>.OkResponseModel(
            code: result.Any() ? ResponseCodeConstants.SUCCESS : ResponseCodeConstants.EMPTY_RESULT,
            data: result,
            message: result.Any() ? "Lấy danh sách file version thành công." : "Không có file version nào."
        ));
    }

    public async Task<IResult> GetDetail([FromServices] ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new GetDesignVersionDetailQuery(id));
        return TypedResults.Ok(BaseResponseModel<DesignVersionHistoryDTO>.OkResponseModel(
            code: ResponseCodeConstants.SUCCESS,
            data: result,
            message: "Lấy chi tiết file version thành công."
        ));
    }

    public async Task<IResult> UpdatePrintable([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateDesignVersionPrintableCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
            code: ResponseCodeConstants.UPDATED,
            data: result,
            message: "Cập nhật trạng thái có thể in thành công."
        ));
    }
}
