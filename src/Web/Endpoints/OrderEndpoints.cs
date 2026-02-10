using Microsoft.AspNetCore.Http.HttpResults;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class OrderEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/order")
                       .WithTags("Order")
                       .WithOpenApi();
        group.MapPost("/query", Query);
        //group.MapPost("/add", Create);
        group.MapGet("/detail/{id}", GetDetail);
        group.MapPut("/update/{id}", Update);
    }

    public async Task<IResult> Query (ISender sender)
    {
        return TypedResults.Ok();
    }

    public async Task<IResult> GetDetail(ISender sender, Guid guid)
    {
        return TypedResults.Ok();
    }

    //public async Task<IResult> Create(ISender sender)
    //{
    //    return TypedResults.Ok();
    //}

    public async Task<IResult> Update(ISender sender)
    {
        return TypedResults.Ok();
    }
}
