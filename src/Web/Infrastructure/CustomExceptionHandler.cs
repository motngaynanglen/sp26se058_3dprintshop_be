using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
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
        var current = exception;
        while (current != null)
        {
            if (_exceptionHandlers.TryGetValue(current.GetType(), out var handler))
            {
                await handler.Invoke(httpContext, current);
                return true;
            }

            current = current.InnerException;
        }

        await HandleUnexpectedException(httpContext, exception);
        return true;
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (ValidationException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        var message = string.Join(" ", exception.Errors.SelectMany(kvp => kvp.Value));

        await httpContext.Response.WriteAsJsonAsync(new BaseResponseModel<object>(
            StatusCodes.Status422UnprocessableEntity,
            ResponseCodeConstants.INVALID_INPUT,
            null,
            new { errors = exception.Errors },
            message));
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

    private async Task HandleUnexpectedException(HttpContext httpContext, Exception ex)
    {
        var root = ex;
        while (root.InnerException != null)
        {
            root = root.InnerException;
        }

        var isDatabaseUnavailable = root.GetType().FullName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true
            || ex.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase);

        httpContext.Response.StatusCode = isDatabaseUnavailable
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status500InternalServerError;

        var message = isDatabaseUnavailable
            ? "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau."
            : "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";

        await httpContext.Response.WriteAsJsonAsync(new BaseResponseModel<object>(
            httpContext.Response.StatusCode,
            ResponseCodeConstants.INTERNAL_SERVER_ERROR,
            null,
            null,
            message));
    }
}
