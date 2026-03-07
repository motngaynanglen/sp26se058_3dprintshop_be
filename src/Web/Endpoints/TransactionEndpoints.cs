using BG_IMPACT.Business.Command.Transaction.Commands;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using PayOS.Models.Webhooks;
using sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
using sp26se058_3dprintshop_be.Application.Auths.Commands.Register;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Infrastructure.Identity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class TransactionEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transaction")
                       .WithTags("Transaction")
                       .WithOpenApi()
                       .RequireCors("AllowFrontend"); // Ép Swagger phải nhận diện group này

        //group.MapPost("/system-login", SystemLogin);
        //group.MapPost("/login", Login);
        //group.MapPost("/register", Register);
    }
    public async Task<IResult> SystemLogin([FromServices] ISender sender, [FromBody] SystemLoginCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<ResponseLoginModel>.OkResponseModel(
                    data: result,
                    message: "Đăng nhập thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (UnauthorizedAccessException)
        {
            // Trả về 401 Unauthorized
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(null, code: ResponseCodeConstants.INVALID_CREDENTIALS),
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }
    public async Task<IResult> HandlePayOSWebhook([FromServices] ISender sender, [FromBody] ProcessOnlinePaymentCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel.OkResponseModel(
                    data: result,
                    message: "Đăng nhập thành công!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (UnauthorizedAccessException)
        {
            // Trả về 401 Unauthorized
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(null, code: ResponseCodeConstants.INVALID_CREDENTIALS),
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }
}
