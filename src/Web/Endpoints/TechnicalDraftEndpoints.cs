
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.DesignLogs.Commands;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using sp26se058_3dprintshop_be.Application.TechnicalDrafts.Commands;
using sp26se058_3dprintshop_be.Application.TechnicalDrafts.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class TechnicalDraftEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/technical-draft")
                       .WithTags("Technical Draft")
                       .WithOpenApi();
        group.MapPost("/", CreateTechnicalDraft)
             .WithSummary("[Staff] Tạo bản nháp kỹ thuật mới")
             .WithDescription("Tạo thông số in và báo giá dự kiến cho một phiên bản thiết kế. Chỉ dành cho Staff.");

        group.MapGet("/my-drafts", GetMyDrafts)
        .WithSummary("[Customer] Lấy danh sách bản thảo kỹ thuật của tôi")
        .WithDescription("Khách hàng xem các báo giá kỹ thuật mà nhân viên đã tạo cho mình.");
        //group.MapGet("/version/{versionId}", GetByVersion)
        //     .WithSummary("[All Roles] Lấy danh sách draft theo phiên bản thiết kế")
        //     .WithDescription("Lấy tất cả các bản nháp kỹ thuật đã tạo cho một DesignVersionHistory cụ thể.");

        //group.MapGet("/{id}", GetById)
        //     .WithSummary("[All Roles] Lấy chi tiết bản nháp kỹ thuật")
        //     .WithDescription("Lấy thông tin chi tiết về thông số in và vật liệu của một Technical Draft.");
    }

    public async Task<IResult> CreateTechnicalDraft([FromServices] ISender sender, [FromBody] CreateTechnicalDraftCommand request)
    {
        var result = await sender.Send(request);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: result,
                message: "Tạo bản nháp kỹ thuật và cập nhật trạng thái dự án thành công."
            ));
    }
    public async Task<IResult> GetMyDrafts([FromServices] ISender sender, [AsParameters] GetMyTechnicalDraftsQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(BaseResponseModel<IEnumerable<TechnicalDraftDTO>>.ListResponseModel(
                data: result.Items,
                additionalData: new { pagination = result.Metadata },
                successMessage: "Lấy danh sách bản thảo thành công."
            ));
    }
    //public async Task<IResult> GetByVersion([FromServices] ISender sender, [FromRoute] Guid versionId)
    //{
    //    // Giả định bạn có Query này để lấy danh sách draft của 1 version
    //    var result = await sender.Send(new GetTechnicalDraftsByVersionQuery(versionId));
    //    return TypedResults.Ok(BaseResponseModel<List<TechnicalDraftDTO>>.ListResponseModel(
    //            data: result,
    //            emptyMessage: "Không tìm thấy bản nháp nào cho phiên bản này."
    //        ));
    //}

    //public async Task<IResult> GetById([FromServices] ISender sender, [FromRoute] Guid id)
    //{
    //    // Giả định bạn có Query lấy chi tiết theo ID
    //    var result = await sender.Send(new GetTechnicalDraftByIdQuery(id));
    //    return TypedResults.Ok(BaseResponseModel<TechnicalDraftDTO>.OkResponseModel(
    //            code: ResponseCodeConstants.SUCCESS,
    //            data: result,
    //            message: "Lấy thông tin chi tiết thành công"
    //        ));
    //}
}
