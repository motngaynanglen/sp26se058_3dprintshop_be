namespace sp26se058_3dprintshop_be.Infrastructure.Service;

internal static class PublicUploadUrlHelper
{
    public static bool TryGetUploadRelativePath(string? url, out string path)
    {
        path = "";
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        var pathPart = trimmed;

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var abs))
            pathPart = abs.AbsolutePath;
        else if (trimmed.StartsWith('/'))
            pathPart = trimmed.Split('?')[0];
        else
            return false;

        if (!pathPart.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return false;

        path = pathPart;
        return true;
    }
}
