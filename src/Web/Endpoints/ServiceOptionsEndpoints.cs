using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
using sp26se058_3dprintshop_be.Application.ServiceOptions.Queries;
using sp26se058_3dprintshop_be.Application.ServicePackages.Queries;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class ServiceOptionsEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/service-option")
                       .WithTags("ServiceOption")
                       .WithOpenApi();


        // --- QUERIES ---
        group.MapGet("/all", QueryAllServiceOptions)
                .WithSummary("[All] Lấy danh sách tùy chọn dịch vụ.");

        // --- COMMANDS ---
        group.MapPost("/add", CreateServiceOption)
                .WithSummary("[Staff/Manager] Tạo tùy chọn dịch vụ mới.");

        group.MapPatch("/{id}/update", UpdateServiceOption)
                .WithSummary("[Staff/Manager] Cập nhật thông tin tùy chọn.");

        group.MapPatch("/{id}/active", ActiveServiceOption)
                .WithSummary("[Staff/Manager] Kích hoạt tùy chọn.");

        group.MapPatch("/{id}/deactive", DeactiveServiceOption)
                .WithSummary("[Staff/Manager] Ngưng kích hoạt tùy chọn.");

        group.MapDelete("/{id}/delete", DeleteServiceOption)
               .WithSummary("[Staff/Manager] Xóa tùy chọn dịch vụ.");
    }

    // 1. Tạo mới
    public async Task<IResult> CreateServiceOption([FromServices] ISender sender, [FromBody] CreateServiceOptionCommand command)
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
    // 2. Truy vấn danh sách
    public async Task<IResult> QueryAllServiceOptions([FromServices] ISender sender)
    {
        try
        {
            var result = await sender.Send(new GetServiceOptionsQuery());
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result,
                    message: "Lấy danh sách tùy chọn thành công"
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    // 3. Cập nhật
    public async Task<IResult> UpdateServiceOption(Guid id, [FromServices] ISender sender, [FromBody] UpdateServiceOptionCommand command)
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
    // 4. Xóa
    public async Task<IResult> DeleteServiceOption(Guid id, [FromServices] ISender sender)
    {
        try
        {
            var result = await sender.Send(new DeleteServiceOptionCommand(id));
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

    // 5. Kích hoạt (Active)
    public async Task<IResult> ActiveServiceOption(Guid id, [FromServices] ISender sender)
    {
        try
        {
            var result = await sender.Send(new ActiveServiceOptionCommand(id));
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

    // 6. Ngưng kích hoạt (Deactive)
    public async Task<IResult> DeactiveServiceOption(Guid id, [FromServices] ISender sender)
    {
        try
        {
            var result = await sender.Send(new DeactiveServiceOptionCommand(id));
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
