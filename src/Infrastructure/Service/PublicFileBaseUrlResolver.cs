using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public sealed class PublicFileBaseUrlResolver : IPublicFileBaseUrlResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly FileUploadOptions _opts;

    public PublicFileBaseUrlResolver(
        IHttpContextAccessor httpContextAccessor,
        IOptions<FileUploadOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _opts = options.Value;
    }

    public string GetBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_opts.PublicBaseUrl))
            return _opts.PublicBaseUrl.TrimEnd('/');

        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is not null)
        {
            var req = ctx.Request;
            var scheme = req.Headers["X-Forwarded-Proto"].FirstOrDefault()
                ?? req.Scheme;
            var host = req.Headers["X-Forwarded-Host"].FirstOrDefault()
                ?? req.Host.Value;
            if (!string.IsNullOrWhiteSpace(host))
                return $"{scheme}://{host}".TrimEnd('/');
        }

        return "http://localhost:5080";
    }
}
