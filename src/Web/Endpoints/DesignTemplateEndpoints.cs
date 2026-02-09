
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.DesignTags.Queries;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class DesignTemplateEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/design-template")
                       .WithTags("DesignTemplate")
                       .WithOpenApi();
        group.MapPost("/query", Query);
        group.MapPost("/add", Create);
        group.MapGet("/detail/{id}", GetDetail);
        group.MapGet("/{id}/tags", GetTags);
        group.MapPut("/update/{id}", Update);
    }

    public async Task<IResult> GetTags(ISender sender, Guid id)
    {
        try
        {
            var result = await sender.Send(new GetDesignTagsListQuery
            {
                DesignTemplateId = id
            });
            return TypedResults.Ok(BaseResponseModel<IEnumerable<DesignTagDTO>>.OkResponseModel(
                    data: result,
                    message: "Lấy tags mẫu thiết kế thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                    data: ex.Message,
                    message: "Lấy tags mẫu thiết kế thất bại!"
                ));
        }
    }

    public async Task<IResult> Query(ISender sender,GetDesignTemplatesWithPaginationQuery query)
    {
        try
        {
            var result = await sender.Send(query);
            return TypedResults.Ok(BaseResponseModel<IEnumerable<DesignTemplateDTO>>.OkResponseModel(
                    code: ResponseCodeConstants.SUCCESS,
                    data: result.Items,
                    additionalData: new { pagination = result.Metadata },
                    message: "Truy vấn mẫu thiết kế thành công!"
                ));

        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                    data: ex.Message,
                    message: "Truy vấn mẫu thiết kế thất bại!"
                ));
        }
    }

    public async Task<IResult> Create(ISender sender, CreateDesignTemplateCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                    data: result,
                    message: "Tạo mẫu thiết kế thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }catch(Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                    data: ex.Message,
                    message: "Tạo mẫu thiết kế thất bại!"
                ));
        }
    }

    public async Task<IResult> GetDetail(ISender sender, Guid id)
    {
        try
        {
            var result = await sender.Send(new GetDesignTemplateDetailQuery
            {
                Id = id
            });
            return TypedResults.Ok(BaseResponseModel<DesignTemplateDTO>.OkResponseModel(
                    data: result,
                    message: "Lấy chi tiết mẫu thiết kế thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(BaseResponseModel<object>.NotFoundResponseModel(null));
        }
    }

    public async Task<IResult> Update(ISender sender, UpdateDesignTemplateCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                    data: result,
                    message: "Cập nhật mẫu thiết kế thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                    data: ex.Message,
                    message: "Cập nhật mẫu thiết kế thất bại!"
                ));
        }
    }
}
