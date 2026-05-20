using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
using sp26se058_3dprintshop_be.Application.ServiceOptions.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class ServiceOptionsEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/service-option")
                       .WithTags("Service Option")
                       .WithOpenApi();

        group.MapGet("/active", QueryActiveServiceOptions)
                .WithSummary("[Customer] Lấy danh sách tùy chọn dịch vụ đang mở bán.");

        group.MapGet("/all", QueryAllServiceOptions)
                .WithSummary("[Staff/Manager] Lấy danh sách tùy chọn dịch vụ có phân trang và bộ lọc.");

        group.MapPost("/all", QueryAllServiceOptionsPost)
                .WithSummary("[Staff/Manager] Lấy danh sách tùy chọn dịch vụ có phân trang và bộ lọc.");

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

    public async Task<IResult> CreateServiceOption([FromServices] ISender sender, [FromBody] CreateServiceOptionCommand command)
    {
        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.CREATED,
                data: result,
                message: "Thêm thành công!"
            ));
    }

    public async Task<IResult> QueryActiveServiceOptions([FromServices] ISender sender)
    {
        var result = await sender.Send(new GetServiceOptionsQuery());

        return TypedResults.Ok(
            BaseResponseModel<IEnumerable<ServiceOptionDTO>>
                .ListResponseModel(data: result, successMessage: "Lấy danh sách tùy chọn đang mở bán thành công")
        );
    }

    public async Task<IResult> QueryAllServiceOptions(
        [FromServices] ISender sender,
        [AsParameters] GetServiceOptionsWithPaginationQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(
            BaseResponseModel<IEnumerable<ServiceOptionDTO>>
                .ListResponseModel(data: result.Items, additionalData: new { paging = result.Metadata }, successMessage: "Lấy danh sách tùy chọn thành công")
        );
    }

    public async Task<IResult> QueryAllServiceOptionsPost(
        [FromServices] ISender sender,
        [FromBody] GetServiceOptionsWithPaginationQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(
            BaseResponseModel<IEnumerable<ServiceOptionDTO>>
                .ListResponseModel(data: result.Items, additionalData: new { paging = result.Metadata }, successMessage: "Lấy danh sách tùy chọn thành công")
        );
    }

    public async Task<IResult> UpdateServiceOption(Guid id, [FromServices] ISender sender, [FromBody] UpdateServiceOptionCommand command)
    {
        var finalCommand = command with { Id = id };
        var result = await sender.Send(finalCommand);

        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: result,
                message: "Cập nhật thành công!"
            ));
    }

    public async Task<IResult> DeleteServiceOption(Guid id, [FromServices] ISender sender)
    {
        var result = await sender.Send(new DeleteServiceOptionCommand(id));
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.DELETED,
                data: result,
                message: "Xóa thành công!"
            ));
    }

    public async Task<IResult> ActiveServiceOption(Guid id, [FromServices] ISender sender)
    {
        var result = await sender.Send(new ActiveServiceOptionCommand(id));
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: result,
                message: "Đã kích hoạt tùy chọn!"
            ));
    }

    public async Task<IResult> DeactiveServiceOption(Guid id, [FromServices] ISender sender)
    {
        var result = await sender.Send(new DeactiveServiceOptionCommand(id));
        return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                code: ResponseCodeConstants.UPDATED,
                data: result,
                message: "Đã ngưng kích hoạt tùy chọn!"
            ));
    }
}
