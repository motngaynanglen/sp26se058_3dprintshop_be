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

        group.MapPost("/system-login", SystemLogin)
                .WithSummary("[Admin] Đăng nhập hệ thống.");
        group.MapPost("/login", Login)
                .WithSummary("[Customer/Staff/Manager] Đăng nhập hệ thống.");
        group.MapPost("/register", Register)
                .WithSummary("[Guest] Đăng kí tài khoản.");
        group.MapPatch("/forgot-password", ForgetPassword)
                .WithSummary("[Guest] Xin mã để đặt lại mật khẩu.");
        group.MapPatch("/reset-password", ResetPassword)
                .WithSummary("[Guest] Đặt lại mật khẩu.");
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
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
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
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> ForgetPassword([FromServices] ISender sender, [FromBody] ForgetPasswordCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                    data: result,
                    message: "Gửi mã thành công. Xin kiểm tra hộp thư!",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> ResetPassword([FromServices] ISender sender, [FromBody] ResetPasswordCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(BaseResponseModel<object>.OkResponseModel(
                    data: result,
                    message: "Đặt lại mật khẩu thành công.",
                    code: ResponseCodeConstants.SUCCESS
                ));
        }
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }
    }
    public async Task<IResult> Register([FromServices] ISender sender, [FromBody] RegisterCommand command)
    {
        try
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
        catch (Exception ex)
        {
            return TypedResults.Json(
                BaseResponseModel<object>.BadRequestResponseModel(ex.Message, code: ResponseCodeConstants.NOT_FOUND),
                statusCode: StatusCodes.Status404NotFound);
        }

    }

}
