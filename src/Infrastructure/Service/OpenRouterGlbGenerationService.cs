using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Infrastructure.OpenRouter;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public sealed class OpenRouterGlbGenerationService : IOpenRouterGlbGenerationService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenRouterGlbGenerationService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiKey = configuration["OpenRouter:ApiKey"] ?? string.Empty;
        _model = configuration["OpenRouter:Model"] ?? "google/gemini-2.0-flash-001";
    }

    public async Task<byte[]> GenerateGlbAsync(
        string prompt,
        string imageBase64,
        string imageMimeType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OpenRouter:ApiKey chưa cấu hình trong appsettings.");

        const string systemPrompt = """
            You are a 3D scene planner. Return ONLY valid JSON:
            {"primitives":[{"type":"box|sphere|cylinder","center":[x,y,z],"size":[w,h,d],"radius":0.5,"height":1,"segments":16,"color":[r,g,b],"rotationEulerDeg":[x,y,z]}]}
            Derive simple primitives from the user prompt and reference image. Max 12 primitives.
            """;

        var body = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = new { url = $"data:{imageMimeType};base64,{imageBase64}" } }
                    }
                }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenRouter lỗi ({(int)resp.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        var sceneJson = ExtractJsonObject(content);
        var scene = JsonSerializer.Deserialize<AiSceneJson>(
            sceneJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return AiSceneToGlbComposer.Build(scene);
    }

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }
}
