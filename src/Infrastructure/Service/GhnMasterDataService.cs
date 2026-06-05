using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class GhnMasterDataService : IGhnMasterDataService
{
    private static readonly ConcurrentDictionary<int, HashSet<int>> ProvinceDistrictCache = new();

    private readonly HttpClient _http;
    private readonly GhnSettings _settings;
    private readonly ILogger<GhnMasterDataService> _logger;

    public GhnMasterDataService(
        HttpClient http,
        IOptions<GhnSettings> settings,
        ILogger<GhnMasterDataService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<GhnProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default) =>
        FetchListAsync(
            "/master-data/province",
            null,
            el => new GhnProvinceDto(
                el.TryGetProperty("ProvinceID", out var id) ? id.GetInt32() : el.GetProperty("province_id").GetInt32(),
                el.TryGetProperty("ProvinceName", out var n) ? n.GetString() ?? "" : el.GetProperty("province_name").GetString() ?? ""),
            cancellationToken);

    public Task<IReadOnlyList<GhnDistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default) =>
        FetchListAsync(
            "/master-data/district",
            new { province_id = provinceId },
            el => new GhnDistrictDto(
                el.TryGetProperty("DistrictID", out var id) ? id.GetInt32() : el.GetProperty("district_id").GetInt32(),
                el.TryGetProperty("DistrictName", out var n) ? n.GetString() ?? "" : el.GetProperty("district_name").GetString() ?? ""),
            cancellationToken);

    public Task<IReadOnlyList<GhnWardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default) =>
        FetchListAsync(
            "/master-data/ward",
            new { district_id = districtId },
            el => new GhnWardDto(
                el.TryGetProperty("WardCode", out var c) ? c.GetString() ?? "" : el.GetProperty("ward_code").GetString() ?? "",
                el.TryGetProperty("WardName", out var n) ? n.GetString() ?? "" : el.GetProperty("ward_name").GetString() ?? ""),
            cancellationToken);

    public async Task<bool> IsDistrictInProvinceAsync(
        int districtId,
        int provinceId,
        CancellationToken cancellationToken = default)
    {
        if (districtId <= 0 || provinceId <= 0)
            return false;

        if (!ProvinceDistrictCache.TryGetValue(provinceId, out var districts))
        {
            var list = await GetDistrictsAsync(provinceId, cancellationToken);
            districts = list.Select(d => d.DistrictId).ToHashSet();
            ProvinceDistrictCache[provinceId] = districts;
        }

        return districts.Contains(districtId);
    }

    private async Task<IReadOnlyList<T>> FetchListAsync<T>(
        string path,
        object? body,
        Func<JsonElement, T> map,
        CancellationToken cancellationToken)
    {
        if (!_settings.IsConfigured)
            throw new InvalidOperationException("GHN chua cau hinh Token/ShopId.");

        using var req = CreateRequest(HttpMethod.Post, path, body);
        using var res = await _http.SendAsync(req, cancellationToken);
        var json = await res.Content.ReadAsStringAsync(cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("[GHN] Master data {Path} HTTP {Status}: {Body}", path, res.StatusCode, json);
            throw new InvalidOperationException("Khong tai duoc danh muc dia chi GHN.");
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("code", out var codeEl) && codeEl.GetInt32() != 200)
        {
            var msg = root.TryGetProperty("message", out var m) ? m.GetString() : json;
            throw new InvalidOperationException(msg ?? "GHN master data loi.");
        }

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return Array.Empty<T>();

        return data.EnumerateArray().Select(map).ToList();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var req = new HttpRequestMessage(method, $"{baseUrl}{path}");
        req.Headers.TryAddWithoutValidation("Token", _settings.Token.Trim());
        if (body != null)
            req.Content = JsonContent.Create(body);
        return req;
    }
}
