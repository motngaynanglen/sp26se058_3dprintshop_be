
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.AppSupports.Queries;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class AppSupportEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/app-support")
                        .WithTags("App Support")
                        .WithOpenApi();

        group.MapGet("/catalog", GetCatalog)
                .WithSummary("[FE Support] Lấy toàn bộ danh mục status/type/role")
                .WithDescription("Trả về toàn bộ danh sách trạng thái, loại nghiệp vụ và vai trò để FE dùng cho dropdown/filter.");

        group.MapGet("/statuses", GetStatuses)
                .WithSummary("[FE Support] Lấy danh sách nhóm status")
                .WithDescription("Trả về các nhóm status: design-work, invoice, order-item, order, shipment, transaction.");

        group.MapGet("/statuses/{groupKey}", GetStatusByGroup)
                .WithSummary("[FE Support] Lấy danh sách status theo nhóm")
                .WithDescription("Ví dụ groupKey: design-work, invoice, order-item, order, shipment, transaction.");

        group.MapGet("/types", GetTypes)
                .WithSummary("[FE Support] Lấy danh sách nhóm type")
                .WithDescription("Trả về các nhóm type: design-log, design-relationship, inventory-transaction, payment-method, source.");

        group.MapGet("/types/{groupKey}", GetTypeByGroup)
                .WithSummary("[FE Support] Lấy danh sách type theo nhóm")
                .WithDescription("Ví dụ groupKey: design-log, design-relationship, inventory-transaction, payment-method, source.");

        group.MapGet("/roles", GetRoles)
                .WithSummary("[FE Support] Lấy danh sách role")
                .WithDescription("Trả về role hiện có trong hệ thống để FE dùng cho phân quyền và quản lý tài khoản.");

        // API dành riêng cho hình ảnh (Preview, Avatar, Minh họa)
        group.MapGet("/presigned-image-url", GetPresignedImageUrl)
                .WithSummary("Lấy link upload ảnh (Max 5MB); Trước mắt không bắt giới hạn.")
                .WithDescription("(Hỗ trợ: .jpg, .png, .webp); Trước mắt không bắt giới hạn dung lượng, nhớ gửi đúng TÊN FILE kèm ĐUÔI để đăng kí đúng link.");

        // API dành cho file 3D nặng (GLB, STL)
        group.MapGet("/presigned-model-url", GetPresignedModelUrl)
                .WithSummary("Lấy link upload file 3D (Max 100MB)")
                .WithDescription("(Hỗ trợ: .glb, .stl); Trước mắt không bắt giới hạn dung lượng, nhớ gửi đúng TÊN FILE kèm ĐUÔI để đăng kí đúng link.");

        group.MapPost("/test-upload-to-b2", TestUploadToB2)
                .DisableAntiforgery();
    }

    public async Task<IResult> GetCatalog([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetSupportCatalogQuery());

        return TypedResults.Ok(BaseResponseModel<SupportCatalogDTO>.OkResponseModel(
            code: ResponseCodeConstants.SUCCESS,
            data: result,
            message: "Lấy danh mục hỗ trợ thành công"
        ));
    }

    public async Task<IResult> GetStatuses([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetSupportStatusesQuery());

        return TypedResults.Ok(BaseResponseModel<IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>>>.OkResponseModel(
            code: ResponseCodeConstants.SUCCESS,
            data: result,
            message: "Lấy danh sách trạng thái thành công"
        ));
    }

    public async Task<IResult> GetStatusByGroup([FromServices] ISender sender, [FromRoute] string groupKey)
    {
        var result = await sender.Send(new GetSupportStatusGroupQuery(groupKey));

        return TypedResults.Ok(BaseResponseModel<IEnumerable<SupportCatalogItemDTO>>.ListResponseModel(
            data: result,
            successMessage: "Lấy danh sách trạng thái thành công",
            emptyMessage: "Nhóm trạng thái này chưa có dữ liệu"
        ));
    }

    public async Task<IResult> GetTypes([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetSupportTypesQuery());

        return TypedResults.Ok(BaseResponseModel<IReadOnlyDictionary<string, IReadOnlyCollection<SupportCatalogItemDTO>>>.OkResponseModel(
            code: ResponseCodeConstants.SUCCESS,
            data: result,
            message: "Lấy danh sách loại nghiệp vụ thành công"
        ));
    }

    public async Task<IResult> GetTypeByGroup([FromServices] ISender sender, [FromRoute] string groupKey)
    {
        var result = await sender.Send(new GetSupportTypeGroupQuery(groupKey));

        return TypedResults.Ok(BaseResponseModel<IEnumerable<SupportCatalogItemDTO>>.ListResponseModel(
            data: result,
            successMessage: "Lấy danh sách loại nghiệp vụ thành công",
            emptyMessage: "Nhóm loại nghiệp vụ này chưa có dữ liệu"
        ));
    }

    public async Task<IResult> GetRoles([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetSupportRolesQuery());

        return TypedResults.Ok(BaseResponseModel<IEnumerable<SupportCatalogItemDTO>>.ListResponseModel(
            data: result,
            successMessage: "Lấy danh sách vai trò thành công",
            emptyMessage: "Chưa có dữ liệu vai trò"
        ));
    }

    public async Task<IResult> GetPresignedImageUrl([FromQuery] string fileName, [FromServices] IS3StorageService s3Service)
    {
        // Đăng ký giới hạn 5MB cho ảnh
        long maxImageSize = 5 * 1024 * 1024; 
        
        var uploadUrl = await s3Service.GetPresignedUploadUrlAsync(fileName, "images", maxImageSize);

        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            code: ResponseCodeConstants.SUCCESS,
            data: new { UploadUrl = uploadUrl, FileUrl = uploadUrl.Split('?')[0] },
            message: "Lấy link upload ảnh thành công (Hỗ trợ: jpg, png, webp)"
        ));

    }
    public async Task<IResult> GetPresignedModelUrl(
        [FromQuery] string fileName,
        [FromServices] IS3StorageService s3Service)
    {
        // Đăng ký giới hạn 100MB cho file 3D
        long maxModelSize = 100 * 1024 * 1024;

        var uploadUrl = await s3Service.GetPresignedUploadUrlAsync(fileName, "models", maxModelSize);

        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
            code: ResponseCodeConstants.SUCCESS,
            data: new { UploadUrl = uploadUrl, FileUrl = uploadUrl.Split('?')[0] },
            message: "Lấy link upload file 3D thành công (Hỗ trợ: .glb)"
        ));
    }
    public async Task<IResult> TestUploadToB2([FromQuery] string uploadUrl, [FromForm] IFormFile file)
    {

        using var client = new HttpClient();

        // Đọc file vào Stream
        using var stream = file.OpenReadStream();
        var content = new StreamContent(stream);

        // BẮT BUỘC: Content-Type phải khớp (thường là image/jpeg hoặc image/png)
        // CẢI THIỆN: Lấy trực tiếp ContentType của file vừa chọn trên Swagger
        // Ví dụ: Nếu chọn file .png, nó sẽ tự là "image/png"
        // Nếu chọn .webp, nó sẽ tự là "image/webp"
        var mimeType = file.ContentType;

        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

        // Thực hiện lệnh PUT 
        var response = await client.PutAsync(uploadUrl, content);

        if (response.IsSuccessStatusCode)
        {
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: new { StatusCode = (int)response.StatusCode },
                message: "BE Test: Upload lên Backblaze thành công!"
            ));
        }

        var errorDetail = await response.Content.ReadAsStringAsync();
        return TypedResults.BadRequest(BaseResponseModel<object>.BadRequestResponseModel(
            data: new { Detail = errorDetail },
            message: "B2 từ chối file",
            code: ResponseCodeConstants.STORAGE_ERROR
        ));

    }
}
