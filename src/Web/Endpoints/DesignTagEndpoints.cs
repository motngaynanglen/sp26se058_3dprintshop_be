using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.DesignTags.Commands;
using sp26se058_3dprintshop_be.Application.DesignTags.Queries;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class DesignTagEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/design-tag")
                       .WithTags("DesignTag")
                       .WithOpenApi();
        group.MapGet("/{templateId}/tags", GetDesignTagsByIds)
                .WithSummary("[Manager] Lấy tất cả tag của một Template với TemplateID.");
        group.MapPost("/sync",SyncDesignTags)
                .WithSummary("[Manager] Cập nhật tất cả tags của một Template với TemplateID và danh sách TagId.");
       
    }

    //public async Task<IResult> GetAllDesignTags([FromServices] ISender sender, [FromBody] GetDesignTagsListQuery query)
    //{
    //    try
    //    {
    //        var result = await sender.Send(query);
    //        return TypedResults.Ok(
    //            BaseResponseModel<IEnumerable<DesignTagDTO>>.OkResponseModel(
    //                data: result,
    //                message: "Lấy danh sách tag thành công!",
    //                code: ResponseCodeConstants.SUCCESS
    //            ));
    //    }
    //    catch (Exception ex)
    //    {
    //        return TypedResults.BadRequest(
    //            BaseResponseModel<string>.BadRequestResponseModel(
    //                ex.Message,
    //                "Lấy danh sách tag thất bại"
    //            ));
    //    }
    //}
    public async Task<IResult> GetDesignTagsByIds([FromServices] ISender sender, [FromRoute] Guid templateId)
    {
        try
        {
            var result = await sender.Send(new GetDesignTagsByTemplateIdQuery { TemplateId = templateId });
            return TypedResults.Ok(
                BaseResponseModel<IEnumerable<DesignTagDTO>>.OkResponseModel(
                    data: result,
                    message: "Lấy danh sách tag thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(
                BaseResponseModel<string>.BadRequestResponseModel(
                    ex.Message,
                    "Lấy danh sách tag thất bại"
                ));
        }
    }
    public async Task<IResult> SyncDesignTags([FromServices] ISender sender, [FromBody] SyncDesignTagCommand command)
    {
        try
        {
            var id = await sender.Send(command);

            return TypedResults.Ok(
                BaseResponseModel<Guid>.OkResponseModel(
                    data: id,
                    message: "Đồng bộ tag thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(
                BaseResponseModel<string>.BadRequestResponseModel(
                    ex.Message,
                    "Sync tag thất bại"
                ));
        }
    }
}
