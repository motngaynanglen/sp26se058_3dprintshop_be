
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
        group.MapGet("/version/{versionId}", GetByVersion)
            .WithSummary("[Customer/Staff/Manager] Lấy danh sách draft theo phiên bản thiết kế")
            .WithDescription("Lấy tất cả các bản nháp kỹ thuật đã tạo cho một DesignVersionHistory cụ thể.");
        group.MapGet("/design-work/{designWorkId}", GetByDesignWork)
            .WithSummary("[Customer/Staff/Manager] Lấy danh sách draft theo DesignWork");
        group.MapGet("/{id}/detail", GetById)
            .WithSummary("[Customer/Staff/Manager] Lấy chi tiết bản nháp kỹ thuật")
            .WithDescription("Lấy thông tin chi tiết về thông số in và vật liệu của một Technical Draft.");
        group.MapPatch("/{id}/update", Update)
            .WithSummary("[Staff/Manager] Cập nhật bản nháp kỹ thuật chưa xác nhận");
        group.MapDelete("/{id}/delete", Delete)
            .WithSummary("[Staff/Manager] Xóa mềm bản nháp kỹ thuật chưa xác nhận");

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
    public async Task<IResult> GetByVersion([FromServices] ISender sender, [FromRoute] Guid versionId)
    {
        var result = await sender.Send(new GetTechnicalDraftsByVersionQuery(versionId));
        return TypedResults.Ok(BaseResponseModel<IEnumerable<TechnicalDraftDTO>>.ListResponseModel(
                data: result,
                successMessage: "Lấy danh sách bản nháp thành công.",
                emptyMessage: "Không tìm thấy bản nháp nào cho phiên bản này."
            ));
    }

    public async Task<IResult> GetByDesignWork([FromServices] ISender sender, [FromRoute] Guid designWorkId)
    {
        var result = await sender.Send(new GetTechnicalDraftsByDesignWorkQuery(designWorkId));
        return TypedResults.Ok(BaseResponseModel<IEnumerable<TechnicalDraftDTO>>.ListResponseModel(
                data: result,
                successMessage: "Lấy danh sách bản nháp thành công.",
                emptyMessage: "Không tìm thấy bản nháp nào cho DesignWork này."
            ));
    }

    public async Task<IResult> GetById([FromServices] ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new GetTechnicalDraftDetailQuery(id));
        return TypedResults.Ok(BaseResponseModel<TechnicalDraftDTO>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Lấy thông tin chi tiết thành công"
            ));
    }

    public async Task<IResult> Update([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] UpdateTechnicalDraftCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return TypedResults.Ok(BaseResponseModel<TechnicalDraftDTO>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: result,
                message: "Cập nhật bản nháp kỹ thuật thành công"
            ));
    }

    public async Task<IResult> Delete([FromServices] ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new DeleteTechnicalDraftCommand(id));
        return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
                code: ResponseCodeConstants.DELETED,
                data: result,
                message: "Xóa bản nháp kỹ thuật thành công"
            ));
    }
}
