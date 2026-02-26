using Microsoft.AspNetCore.Mvc;
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
                       .WithTags("ConceptTag")
                       .WithOpenApi();

        group.MapGet("/all", Get);
        group.MapPost("/add", Add);
        group.MapPut("/update/{id}", Update);
    }

    public async Task<IResult> Get(
        [FromServices] ISender sender)
    {
        var result = await sender.Send(new GetConceptTagsListQuery());

        return TypedResults.Ok(
            BaseResponseModel<IEnumerable<ConceptTagDTO>>.OkResponseModel(
                data: result,
                message: "Lấy danh sách concept tag thành công",
                code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> Update(
        [FromServices] ISender sender,
        [FromRoute] Guid id,
        [FromBody] UpdateConceptTagCommand command)
    {
        var finalCommand = command with { Id = id };

        var result = await sender.Send(finalCommand);

        return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
            data: result,
            message: "Cập nhật Concept tag thành công!",
            code: ResponseCodeConstants.SUCCESS));
    }

    public async Task<IResult> Add(
        [FromServices] ISender sender,
        [FromBody] CreateConceptTagCommand command)
    {
        var result = await sender.Send(command);

        return TypedResults.Ok(BaseResponseModel<Guid>.OkResponseModel(
            data: result,
            message: "Tạo Concept tag thành công!",
            code: ResponseCodeConstants.SUCCESS));
    }
}
