using System.Net.Http.Json;
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
        _aiUrl = configuration["AI:GenerateUrl"]
                 ?? throw new InvalidOperationException("AI URL is not configured");
    }

    public async Task<byte[]> GenerateModelAsync(string imageBase64)
    {
        var payload = new { image = imageBase64 };

        // Gửi request POST với payload JSON chứa ảnh Base64
        var response = await _httpClient.PostAsJsonAsync(_aiUrl, payload);

        // Kiểm tra lỗi HTTP (404, 500, v.v.)
        response.EnsureSuccessStatusCode();

        // Đọc trực tiếp nội dung phản hồi dưới dạng mảng byte (vì kết quả là file nhị phân glTF)
        return await response.Content.ReadAsByteArrayAsync();
    }
}
