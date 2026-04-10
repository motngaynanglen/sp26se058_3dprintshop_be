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

        group.MapPatch("/{id}/update", UpdateServicePackage)
                .WithSummary("[Staff/Manager] Cập nhật thông tin gói dịch vụ.");

        group.MapPatch("/{id}/active", ActiveServicePackage)
                .WithSummary("[Staff/Manager] Kích hoạt gói dịch vụ.");

        group.MapPatch("/{id}/deactive", DeactiveServicePackage)
                .WithSummary("[Staff/Manager] Ngưng kích hoạt gói dịch vụ.");

        group.MapDelete("/{id}/delete", DeleteServicePackage)
               .WithSummary("[Staff/Manager] Xóa tùy gói dịch vụ.");


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
    public async Task<IResult> UpdateServicePackage(Guid id, [FromServices] ISender sender, [FromBody] UpdateServicePackageCommand command)
    {
        try
        {
            var finalCommand = command with { Id = id };
            var result = await sender.Send(finalCommand);

            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Cập nhật thành công!"
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
    public async Task<IResult> DeleteServicePackage(Guid id, [FromServices] ISender sender)
    {
        try
        {
            var result = await sender.Send(new DeleteServicePackageCommand { Id = id});
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Xóa thành công!"
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }

    }

    public async Task<IResult> ActiveServicePackage(Guid id, [FromServices] ISender sender)
    {
        try
        {
            var result = await sender.Send(new ActiveServicePackageCommand { Id = id });
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Đã kích hoạt tùy chọn!"
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }

    }

    public async Task<IResult> DeactiveServicePackage(Guid id, [FromServices] ISender sender)
    {
        try
        {
            var result = await sender.Send(new DeactiveServicePackageCommand{ Id = id });
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Đã ngưng kích hoạt tùy chọn!"
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.FAILED),
                statusCode: StatusCodes.Status400BadRequest);
        }

    }
}
