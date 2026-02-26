using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
using sp26se058_3dprintshop_be.Application.Auths.Commands.Register;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Infrastructure.Identity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class AuthEndpoints : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
                       .WithTags("Auth")
                       .WithOpenApi()
                       .RequireCors("AllowFrontend"); // Ép Swagger phải nhận diện group này

        group.MapPost("/system-login", SystemLogin);
        group.MapPost("/login", Login);
        group.MapPost("/register", Register);
        //app.MapGroup(this)
        //    .MapPost(SystemLogin, "system-login") // URL: /api/auth/system-login
        //    .MapPost(Login, "login");             // URL: /api/auth/login
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
    public async Task<IResult> Login([FromServices] ISender sender, [FromBody] LoginCommand command)
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
    public async Task<IResult> Register([FromServices] ISender sender, [FromBody] RegisterCommand command)
    {
        // Gửi command tới RegisterCommandHandler
        var result = await sender.Send(command);

        if (result)
        {
            return TypedResults.Ok(BaseResponseModel<bool>.OkResponseModel(
                data: true,
                message: "Đăng ký tài khoản khách hàng thành công."
            ));
        }

        return TypedResults.BadRequest(BaseResponseModel<bool>.BadRequestResponseModel(
            data: false,
            message: "Đăng ký thất bại, vui lòng thử lại."
        ));
    }

}
