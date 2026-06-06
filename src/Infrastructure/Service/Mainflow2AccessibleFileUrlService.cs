using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Options;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public sealed class Mainflow2AccessibleFileUrlService : IMainflow2AccessibleFileUrlService
{
    private readonly IBackblazeB2Service _b2;
    private readonly BackblazeB2Options _b2Opts;
    private readonly IPublicFileBaseUrlResolver _baseUrlResolver;

    public Mainflow2AccessibleFileUrlService(
        IBackblazeB2Service b2,
        IOptions<BackblazeB2Options> b2Options,
        IPublicFileBaseUrlResolver baseUrlResolver)
    {
        _b2 = b2;
        _b2Opts = b2Options.Value;
        _baseUrlResolver = baseUrlResolver;
    }

    public string? Resolve(string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return fileUrl;

        var url = fileUrl.Trim();
        if (PublicUploadUrlHelper.TryGetUploadRelativePath(url, out var uploadPath))
            return $"{_baseUrlResolver.GetBaseUrl()}{uploadPath}";

        if (IsPrivateB2ObjectUrl(url) && IsB2Configured())
        {
            try
            {
                return _b2.GetPresignedDownloadUrl(url);
            }
            catch
            {
                return url;
            }
        }

        return url;
    }

    public string? RewriteQuoteMetadataJson(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return metadataJson;

        try
        {
            var node = JsonNode.Parse(metadataJson);
            if (node is null)
                return metadataJson;

            if (node["previewGlbUrl"]?.GetValue<string>() is { } previewNew)
                node["previewGlbUrl"] = Resolve(previewNew);
            else if (node["autoPreviewGlbUrl"]?.GetValue<string>() is { } preview)
                node["autoPreviewGlbUrl"] = Resolve(preview);

            if (node["designFileUrls"] is JsonArray arr)
            {
                for (var i = 0; i < arr.Count; i++)
                {
                    if (arr[i]?.GetValue<string>() is { } u)
                        arr[i] = Resolve(u);
                }
            }

            return node.ToJsonString();
        }
        catch
        {
            return metadataJson;
        }
    }

    public string? RewriteStaffQuoteMetadataForDisplay(string? metadataJson, string? sourceType, string? customerFileUrl)
    {
        var rewritten = RewriteQuoteMetadataJson(metadataJson);
        if (!string.Equals(sourceType, SourceTypes.AiGenerated, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(customerFileUrl))
            return rewritten;

        var json = rewritten ?? metadataJson;
        if (string.IsNullOrWhiteSpace(json))
            return rewritten;

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
                return rewritten;

            var primary = Resolve(customerFileUrl)!;
            node["previewGlbUrl"] = primary;
            node["autoPreviewGlbUrl"] = primary;

            var list = new JsonArray { primary };
            if (node["designFileUrls"] is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    if (item?.GetValue<string>() is not { } u)
                        continue;
                    var resolved = Resolve(u) ?? u;
                    if (IsQuoteTemplatePlaceholder(resolved))
                        continue;
                    if (string.Equals(resolved, primary, StringComparison.OrdinalIgnoreCase))
                        continue;
                    list.Add(resolved);
                }
            }

            node["designFileUrls"] = list;
            return node.ToJsonString();
        }
        catch
        {
            return rewritten;
        }
    }

    private static bool IsQuoteTemplatePlaceholder(string url) =>
        url.Contains("/uploads/mainflow2/", StringComparison.OrdinalIgnoreCase);

    private bool IsB2Configured() =>
        !string.IsNullOrWhiteSpace(_b2Opts.KeyId)
        && !string.IsNullOrWhiteSpace(_b2Opts.ApplicationKey)
        && !string.IsNullOrWhiteSpace(_b2Opts.BucketName);

    private static bool IsPrivateB2ObjectUrl(string url) =>
        url.Contains("backblazeb2.com", StringComparison.OrdinalIgnoreCase)
        || url.Contains("backblaze", StringComparison.OrdinalIgnoreCase);
}
