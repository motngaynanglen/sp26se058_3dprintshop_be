using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using System;

namespace sp26se058_3dprintshop_be.Web.Infrastructure;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;

    public CustomExceptionHandler()
    {
        // Register known exception types and handlers.
        _exceptionHandlers = new()
            {
                { typeof(ValidationException), HandleValidationException },
                { typeof(NotFoundException), HandleNotFoundException },
                { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
                { typeof(ForbiddenAccessException), HandleForbiddenAccessException },
                { typeof(BadHttpRequestException), HandleBadHttpRequestException }, // Xử lý lỗi Binding/Guid sai
            };
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (_exceptionHandlers.ContainsKey(exceptionType))
        {
            await _exceptionHandlers[exceptionType].Invoke(httpContext, exception);
            return true;
        }
        await HandleUnknownException(httpContext, exception);
        return true;
    }
    private async Task HandleUnknownException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new BaseResponseModel(
                statusCode: StatusCodes.Status400BadRequest,
                data: ex.Message,
                message: "Đã xảy ra lỗi hệ thống không mong muốn.",
                code: ResponseCodeConstants.FAILED
            )
        );
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (ValidationException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        await httpContext.Response.WriteAsJsonAsync(
            new BaseResponseModel(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                data: exception.Errors,
                message: exception.Message,
                code: ResponseCodeConstants.UNPROCESSABLE_ENTITY
                )
            );
    }
    private async Task HandleBadHttpRequestException(HttpContext httpContext, Exception ex)
    {
        // xử lý lỗi "Failed to bind parameter" ( sai format)
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;


        await httpContext.Response.WriteAsJsonAsync(
            new BaseResponseModel(
                statusCode: StatusCodes.Status400BadRequest,
                data: ex.Message,
                message: "Định dạng dữ liệu gửi lên không đúng.",
                code: ResponseCodeConstants.INVALID_INPUT
                )
            );
    }
    private async Task HandleNotFoundException(HttpContext httpContext, Exception ex)
    {
        var exception = (NotFoundException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;


        await httpContext.Response.WriteAsJsonAsync(
            new BaseResponseModel(
                statusCode: StatusCodes.Status404NotFound,
                data: exception.Message,
                message: "Không tìm thấy mục tiêu.",
                code: ResponseCodeConstants.NOT_FOUND
                )
            );
    }

    private async Task HandleUnauthorizedAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await httpContext.Response.WriteAsJsonAsync(
            new BaseResponseModel(
                statusCode: StatusCodes.Status404NotFound,
                data: ex.Message,
                message: "Chưa đăng nhập",
                code: ResponseCodeConstants.UNAUTHORIZED
                )
            );
    }

    private async Task HandleForbiddenAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        await httpContext.Response.WriteAsJsonAsync(
            new BaseResponseModel(
                statusCode: StatusCodes.Status403Forbidden,
                data: ex.Message,
                message: "Không có phép sử dụng.",
                code: ResponseCodeConstants.FORBIDDEN
                )
            );
    }
}
