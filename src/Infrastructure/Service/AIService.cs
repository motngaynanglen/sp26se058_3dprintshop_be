using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _aiUrl;

    public AIService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
        _aiUrl = configuration["AI:GenerateUrl"]
                 ?? throw new InvalidOperationException("AI URL is not configured");
    }

    public async Task<byte[]> GenerateModelAsync(string imageBase64)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _aiUrl);
        request.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
        request.Content = JsonContent.Create(new { image = imageBase64 });

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = (await response.Content.ReadAsStringAsync()).Trim();
        if (string.IsNullOrEmpty(body))
            throw new InvalidOperationException("AI Service trả về dữ liệu trống.");

        var glbBase64 = ExtractGlbBase64(body);
        try
        {
            return Convert.FromBase64String(glbBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("AI Service trả về base64 GLB không hợp lệ.", ex);
        }
    }

    private static string ExtractGlbBase64(string body)
    {
        if (body.StartsWith('"'))
        {
            var decoded = JsonSerializer.Deserialize<string>(body);
            if (string.IsNullOrWhiteSpace(decoded))
                throw new InvalidOperationException("AI Service trả về chuỗi base64 rỗng.");
            return StripDataUriPrefix(decoded);
        }

        if (body.StartsWith('{'))
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var key in new[] { "glb", "model", "data", "result", "output", "mesh", "image" })
            {
                if (doc.RootElement.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    var value = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return StripDataUriPrefix(value);
                }
            }

            throw new InvalidOperationException("AI Service trả về JSON không chứa GLB base64.");
        }

        return StripDataUriPrefix(body);
    }

    private static string StripDataUriPrefix(string base64)
    {
        var trimmed = base64.Trim();
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = trimmed.IndexOf(',');
            if (comma >= 0)
                trimmed = trimmed[(comma + 1)..];
        }

        return trimmed.Trim();
    }
}
