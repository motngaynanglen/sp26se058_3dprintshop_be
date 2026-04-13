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
                { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
                { typeof(BadHttpRequestException), HandleBadHttpRequestException },
                { typeof(BusinessException), HandleBusinessException },
            };
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[API Error]: {exception.GetType().Name} - {exception.Message}");
        if (exception is BusinessException businessEx)
        {
            await HandleBusinessException(httpContext, businessEx);
            return true;
        }

        var exceptionType = exception.GetType();

        if (_exceptionHandlers.ContainsKey(exceptionType))
        {
            await _exceptionHandlers[exceptionType].Invoke(httpContext, exception);
            return true;
        }
        await HandleUnknownException(httpContext, exception);
        return true;
    }
    private async Task HandleBusinessException(HttpContext httpContext, Exception ex)
    {
        var businessEx = (BusinessException)ex;
        int statusCode = businessEx switch
        {
            DataNotFoundException => StatusCodes.Status404NotFound,
            ForbiddenAccessException => StatusCodes.Status403Forbidden,
            DuplicateException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new BaseResponseModel(
            statusCode: statusCode,
            data: businessEx.Details,
            message: businessEx.Message,
            code: businessEx.Code ?? ResponseCodeConstants.FAILED
        ));

    }
    private async Task HandleUnknownException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new BaseResponseModel(
                statusCode: StatusCodes.Status500InternalServerError,
                data: ex.Message,
                message: "Đã xảy ra lỗi hệ thống không mong muốn, hoặc lỗi chưa được phân loại.",
                code: ResponseCodeConstants.FAILED
            )
        );
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (ValidationException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        await httpContext.Response.WriteAsJsonAsync(
            new BaseResponseModel<IDictionary<string, string[]>>(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                data: exception.Errors,
                message: exception.Message,
                code: ResponseCodeConstants.VAL_INVALID_INPUT
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
                message: "Định dạng dữ liệu gửi lên (JSON/Primitive Type) không hợp lệ.",
                code: ResponseCodeConstants.VAL_GENERAL
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
                message: "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.",
                code: ResponseCodeConstants.UNAUTHORIZED
                )
            );
    }
    /*private async Task HandleNotFoundException(HttpContext httpContext, Exception ex)
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
    }*/

    /*private async Task HandleForbiddenAccessException(HttpContext httpContext, Exception ex)
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
    }*/
}
