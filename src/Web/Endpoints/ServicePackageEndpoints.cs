using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.ServicePackages.Commands;
using sp26se058_3dprintshop_be.Application.ServicePackages.Queries;
using sp26se058_3dprintshop_be.Domain.Entities;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class ServicePackageEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/service-package")
                       .WithTags("ServicePackage")
                       .WithOpenApi();


        group.MapPost("/query", QueryServicePackages)
                .WithSummary("[All] Lấy danh sách các gói (P1, P2...).");
        group.MapPost("/add", CreateServicePackage)
                .WithSummary("[Staff/Manager] Tạo gói mới.");

        //group.MapPatch("/{id}/toggle-status", SwitchStatus)
        //        .WithSummary("[Staff/Manager] Ẩn/Hiện đánh giá (nếu vi phạm quy tắc cộng đồng).")
        //        .WithDescription("Để trống dữ liệu Request BODY khi gọi");
        //group.MapPatch("/{id}/delete", DeleteFeedback)
        //        .WithSummary("[Customer/Staff/Manager] Xóa đánh giá với ID.")
        //        .WithDescription("Để trống dữ liệu Request BODY khi gọi; Customer sẽ xóa được feedback của bản thân. Staff, Manager có thể xóa mọi feedback");


    }
    public async Task<IResult> CreateServicePackage([FromServices] ISender sender, [FromBody] CreateServicePackageCommand command)
    {
        try
        {
            var result = await sender.Send(command);

            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Thêm thành công!"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> QueryServicePackages([FromServices] ISender sender, [FromBody] GetServicePackagesQuery query)
    {
        try
        {
            var result = await sender.Send(query);

            return TypedResults.Ok(BaseResponseModel<IEnumerable<ServicePackageDTO>>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    //additionalData: new { paging = result.Metadata },
                    message: "Lấy danh sách thành công"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
   
}
