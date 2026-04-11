using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.PackageOptions.Commands;
using sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
using sp26se058_3dprintshop_be.Application.ServiceOptions.Queries;
using sp26se058_3dprintshop_be.Application.ServicePackages.Queries;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class PackageOptionsEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/package-option")
                       .WithTags("PackageOption")
                       .WithOpenApi();


        // --- QUERIES ---
        //group.MapGet("/all", QueryAllServiceOptions)
        //        .WithSummary("[All] Lấy danh sách tùy chọn dịch vụ.");

        // --- COMMANDS ---
        group.MapPost("/add", CreateServiceOption)
                .WithSummary("[Staff/Manager] Thêm tùy chọn dịch vụ mới.");

        group.MapPatch("/{id}/update", UpdateServiceOption)
                .WithSummary("[Staff/Manager] Cập nhật thông tin tùy chọn gói.");

        group.MapDelete("/{id}/delete", DeleteServiceOption)
               .WithSummary("[Staff/Manager] Xóa tùy chọn dịch vụ gói.");
    }

    // 1. Tạo mới
    public async Task<IResult> CreateServiceOption([FromServices] ISender sender, [FromBody] CreatePackageOptionCommand command)
    {

        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Thêm thành công!"
            ));


    }

    public async Task<IResult> UpdateServiceOption(Guid id, [FromServices] ISender sender, [FromBody] UpdatePackageOptionCommand command)
    {

        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Cập nhật thành công!"
            ));

    }
    public async Task<IResult> DeleteServiceOption(Guid id, [FromServices] ISender sender)
    {

        var result = await sender.Send(new DeletePackageOptionCommand { Id = id });
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.SUCCESS,
                data: result,
                message: "Xóa thành công!"
            ));


    }
}
