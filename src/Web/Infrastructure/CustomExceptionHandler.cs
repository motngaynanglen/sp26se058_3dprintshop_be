using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

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

        return false;
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (ValidationException)ex;

        //httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        //await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(exception.Errors)
        //{
        //    Status = StatusCodes.Status400BadRequest,
        //    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        //});
        // Trả về cấu hình liệt kê chi tiết các lỗi
        await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Dữ liệu không hợp lệ",
            Detail = "Một hoặc nhiều lỗi validation đã xảy ra.",
            Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
        });
    }
    private async Task HandleBadHttpRequestException(HttpContext httpContext, Exception ex)
    {
        // Đây là nơi xử lý lỗi "Failed to bind parameter" (Guid sai format)
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Yêu cầu không hợp lệ",
            Detail = "Định dạng dữ liệu gửi lên không đúng.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }
    private async Task HandleNotFoundException(HttpContext httpContext, Exception ex)
    {
        var exception = (NotFoundException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
            Detail = exception.Message
        });
    }

    private async Task HandleUnauthorizedAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        });
    }

    private async Task HandleForbiddenAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        });
    }
}
