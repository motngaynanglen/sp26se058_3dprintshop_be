using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.ConceptTags.Commands;
using sp26se058_3dprintshop_be.Application.ConceptTags.Queries;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class ConceptTagEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/concept-tag")
                       .WithTags("Concept Tag")
                       .WithOpenApi();
        group.MapGet("/all", Get);
        group.MapPost("/add", Add);
        group.MapPut("/update/{id}", Update);
        //group.MapDelete("/delete/{id}", Delete);
    }

    public async Task<IResult> Get(ISender sender)
    {
        try
        {
            var result = await sender.Send(new GetConceptTagsListQuery());

            return TypedResults.Ok(
                BaseResponseModel<IEnumerable<ConceptTagDTO>>.OkResponseModel(
                    data: result,
                    message: "Lấy danh sách concept tag thành công",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(
                BaseResponseModel<string>.BadRequestResponseModel(
                    ex.Message,
                    "Lấy danh sách concept tag thất bại"
                ));
        }
    }

    public async Task<IResult> Update(ISender sender, Guid id, UpdateConceptTagCommand command) {
        try
        {
            var finalCommand = command with { Id = id };
            var result = await sender.Send(finalCommand);
            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                data: result,
                message: "Cập nhật Concept tag thành công!",
                code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex) {
            return TypedResults.BadRequest(BaseResponseModel<string>.BadRequestResponseModel(
                data: ex.Message,
                message: "Lỗi trong quá trình cập nhật"
            ));
        }
    }

    public async Task<IResult> Add(ISender sender, CreateConceptTagCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
                    data: result,
                    message: "Tạo Concept tag thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Ok(BaseResponseModel<string>.BadRequestResponseModel(
                    data: ex.Message,
                    message: "Tạo Concept tag thất bại"
                    ));
        }
    }

    //public async Task<IResult> Delete()
    //{
    //    // Implementation for deleting a concept tag
    //    return TypedResults.Ok();
    //}
}
