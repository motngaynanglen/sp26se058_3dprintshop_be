using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Mainflow2.Commands;
using sp26se058_3dprintshop_be.Application.Mainflow2.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class Mainflow2Endpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mainflow-2")
            .WithTags("Mainflow 2")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/design-requests", CreateRequest)
            .WithSummary("Mainflow 2 — [Customer] Tạo yêu cầu thiết kế + ảnh ý tưởng.");

        group.MapPost("/print-requests", CreatePrintRequest)
            .WithSummary("Mainflow 2 (cập nhật) — [Customer] Gửi yêu cầu in từ file STL/OBJ của khách + thông số kỹ thuật.");

        group.MapPost("/ai-print-requests", CreateAiPrintRequest)
            .WithSummary("Luồng 3 — [Customer] Gửi mô hình GLB từ AI để KTV báo giá & in (giống flow 2).");

        group.MapGet("/design-requests", ListRequests)
            .WithSummary("Mainflow 2 — [Customer/Staff/Manager] Danh sách yêu cầu (phân quyền theo vai trò).");

        group.MapGet("/design-requests/{id:guid}", GetDetail)
            .WithSummary("Mainflow 2 — Chi tiết, thread tin nhắn và timeline trạng thái.");

        group.MapPost("/design-requests/{id:guid}/messages", AddMessage)
            .WithSummary("Mainflow 2 — Gửi ghi chú / chat (khách hoặc NV được phân công).");

        group.MapPost("/design-requests/{id:guid}/staff/assign", StaffAssign)
            .WithSummary("Mainflow 2 — [Staff] Tiếp nhận yêu cầu.");

        group.MapPost("/design-requests/{id:guid}/manager/assign", ManagerAssign)
            .WithSummary("Mainflow 2 — [Manager] Giao yêu cầu cho nhân viên.");

        group.MapGet("/staff-list", ListStaff)
            .WithSummary("Mainflow 2 — [Manager] Danh sách nhân viên để giao việc.");

        group.MapGet("/printable-designs", ListPrintableDesigns)
            .WithSummary("Mainflow 2 — [Customer] TH2: Thiết kế đã thanh toán — có thể In ngay.");

        group.MapGet("/reprintable-orders", ListReprintableOrders)
            .WithSummary("Mainflow 2 — [Customer] TH3: Đơn in custom đã có báo giá — In lại.");

        group.MapPost("/print-from-design", CreatePrintFromDesign)
            .WithSummary("Mainflow 2 — [Customer] TH2: Tạo yêu cầu in từ thiết kế có sẵn.");

        group.MapPost("/reprint-requests", CreateReprintRequest)
            .WithSummary("Mainflow 2 — [Customer] TH3: In lại đơn custom theo báo giá cũ.");

        group.MapPost("/design-requests/{id:guid}/staff/quote", StaffQuote)
            .WithSummary("Mainflow 2 — [Staff] Gửi báo giá kèm file GLB xem trước (previewGlbUrl hoặc upload multipart).")
            .DisableAntiforgery();

        group.MapPost("/design-requests/{id:guid}/approve", CustomerApprove)
            .WithSummary("Mainflow 2 — [Customer] Chấp nhận báo giá hiện tại.");

        group.MapPost("/design-requests/{id:guid}/staff/complete-design", StaffCompleteDesign)
            .WithSummary("Mainflow 2 — [Staff] Gửi bảng thiết kế sau cọc — mở thanh toán phần còn lại.");

        group.MapPost("/design-requests/{id:guid}/cancel", Cancel)
            .WithSummary("Mainflow 2 — Hủy yêu cầu (khách hoặc NV/manager được phép).");
    }

    private static async Task<IResult> CreatePrintRequest(ISender sender, [FromBody] CreateCustomFilePrintRequestCommand body)
    {
        try
        {
            var id = await sender.Send(body);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: id,
                message: "Đã gửi yêu cầu in 3D. Vui lòng chờ kỹ thuật viên báo giá."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> CreateAiPrintRequest(ISender sender, [FromBody] CreateAiGeneratedPrintRequestCommand body)
    {
        try
        {
            var id = await sender.Send(body);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: id,
                message: "Đã gửi mô hình AI. Kỹ thuật viên sẽ báo giá từ file GLB."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> CreateRequest(ISender sender, [FromBody] CreateMainflow2DesignRequestCommand body)
    {
        try
        {
            var id = await sender.Send(body);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: id,
                message: "Tạo yêu cầu thành công."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ListRequests(
        ISender sender,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] string? status,
        [FromQuery] string? category,
        [FromQuery] string? sortBy,
        [FromQuery] bool? sortDescending)
    {
        try
        {
            var query = new ListMainflow2DesignRequestsQuery
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 10,
                Status = status,
                Category = category,
                SortBy = sortBy,
                SortDescending = sortDescending ?? true,
            };
            var result = await sender.Send(query);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result.Items,
                additionalData: new { paging = result.Metadata },
                message: "Lấy danh sách thành công."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FORBIDDEN),
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> GetDetail(ISender sender, [FromRoute] Guid id)
    {
        try
        {
            var result = await sender.Send(new GetMainflow2DesignDetailQuery { DesignWorkId = id });
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Lấy chi tiết thành công."));
        }
        catch (Exception ex)
        {
            var code = ex is UnauthorizedAccessException ? StatusCodes.Status403Forbidden : StatusCodes.Status404NotFound;
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: code);
        }
    }

    private static async Task<IResult> AddMessage(ISender sender, [FromRoute] Guid id, [FromBody] AddMainflow2DesignMessageCommand body)
    {
        try
        {
            var cmd = body with { DesignWorkId = id };
            var logId = await sender.Send(cmd);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: logId,
                message: "Đã gửi tin nhắn."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> StaffAssign(ISender sender, [FromRoute] Guid id)
    {
        try
        {
            await sender.Send(new StaffAssignMainflow2DesignCommand { DesignWorkId = id });
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: true,
                message: "Đã tiếp nhận yêu cầu."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ManagerAssign(
        ISender sender,
        [FromRoute] Guid id,
        [FromBody] ManagerAssignMainflow2DesignCommand body)
    {
        try
        {
            await sender.Send(body with { DesignWorkId = id });
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: true,
                message: "Đã giao việc cho nhân viên."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ListStaff(ISender sender)
    {
        try
        {
            var result = await sender.Send(new ListMainflow2StaffQuery());
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Lấy danh sách nhân viên thành công."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FORBIDDEN),
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> ListPrintableDesigns(ISender sender)
    {
        try
        {
            var result = await sender.Send(new ListPrintableDesignsQuery());
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Lấy danh sách thiết kế in được thành công."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ListReprintableOrders(ISender sender)
    {
        try
        {
            var result = await sender.Send(new ListReprintableDesignWorksQuery());
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Lấy danh sách đơn in lại được thành công."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> CreatePrintFromDesign(ISender sender, [FromBody] CreatePrintFromDesignCommand body)
    {
        try
        {
            var id = await sender.Send(body);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: id,
                message: "Đã tạo yêu cầu in. Thanh toán như Pre-Order — theo dõi trong Đơn của tôi."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> CreateReprintRequest(ISender sender, [FromBody] CreateReprintRequestCommand body)
    {
        try
        {
            var id = await sender.Send(body);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: id,
                message: "Đã tạo yêu cầu in lại. Thanh toán như Pre-Order — theo dõi trong Đơn của tôi."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> StaffQuote(
        ISender sender,
        IPublicFileStorageService storage,
        IOptions<FileUploadOptions> uploadOptions,
        [FromRoute] Guid id,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            StaffSubmitMainflow2QuoteCommand cmd;

            if (httpContext.Request.HasFormContentType)
            {
                var form = await httpContext.Request.ReadFormAsync(cancellationToken);
                var glbFile = form.Files.GetFile("previewGlbFile");
                string? previewGlbUrl = form["previewGlbUrl"].FirstOrDefault();

                if (glbFile is { Length: > 0 })
                {
                    previewGlbUrl = await SaveQuoteGlbAsync(glbFile, storage, uploadOptions.Value, cancellationToken);
                }

                cmd = new StaffSubmitMainflow2QuoteCommand
                {
                    DesignWorkId = id,
                    QuotedPrice = decimal.TryParse(form["quotedPrice"].FirstOrDefault(), out var price) ? price : 0,
                    Currency = form["currency"].FirstOrDefault() ?? "VND",
                    StaffNote = form["staffNote"].FirstOrDefault(),
                    LaborCost = decimal.TryParse(form["laborCost"].FirstOrDefault(), out var labor) ? labor : 0,
                    PreviewGlbUrl = previewGlbUrl,
                };
            }
            else
            {
                var body = await httpContext.Request.ReadFromJsonAsync<StaffSubmitMainflow2QuoteCommand>(cancellationToken)
                    ?? throw new InvalidOperationException("Body báo giá không hợp lệ.");
                cmd = body with { DesignWorkId = id };
            }

            await sender.Send(cmd, cancellationToken);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: true,
                message: "Đã gửi báo giá & file GLB thiết kế."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<string> SaveQuoteGlbAsync(
        IFormFile file,
        IPublicFileStorageService storage,
        FileUploadOptions opts,
        CancellationToken cancellationToken)
    {
        if (opts.MaxBytes > 0 && file.Length > opts.MaxBytes)
            throw new InvalidOperationException($"File GLB vượt quá giới hạn {opts.MaxBytes} bytes.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".glb")
            throw new InvalidOperationException("previewGlbFile phải là file .glb.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        return await storage.SavePublicFileAsync(
            ms.ToArray(),
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "model/gltf-binary" : file.ContentType,
            "mainflow2/quotes",
            cancellationToken);
    }

    private static async Task<IResult> CustomerApprove(ISender sender, [FromRoute] Guid id)
    {
        try
        {
            await sender.Send(new CustomerApproveMainflow2DesignCommand { DesignWorkId = id });
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: true,
                message: "Đã chấp nhận báo giá."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> StaffCompleteDesign(
        ISender sender,
        [FromRoute] Guid id,
        [FromBody] StaffCompleteMainflow2DesignCommand? body)
    {
        try
        {
            await sender.Send(new StaffCompleteMainflow2DesignCommand
            {
                DesignWorkId = id,
                DeliverableFileUrl = body?.DeliverableFileUrl ?? string.Empty,
                Note = body?.Note,
            });
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: true,
                message: "Đã gửi bảng thiết kế — khách có thể hoàn tất thanh toán."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> Cancel(ISender sender, [FromRoute] Guid id)
    {
        try
        {
            await sender.Send(new CancelMainflow2DesignCommand { DesignWorkId = id });
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: true,
                message: "Đã hủy yêu cầu."));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
