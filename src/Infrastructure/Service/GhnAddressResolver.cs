using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class GhnAddressResolver : IGhnAddressResolver
{
    private static readonly Regex PrefixRegex = new(
        @"^(tinh|tp\.?|thanh pho|quan|huyen|thi xa|phuong|xa|thi tran|p\.|q\.|tx\.)\s+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IGhnMasterDataService _ghn;
    private readonly ILogger<GhnAddressResolver> _logger;

    public GhnAddressResolver(IGhnMasterDataService ghn, ILogger<GhnAddressResolver> logger)
    {
        _ghn = ghn;
        _logger = logger;
    }

    public async Task<GhnAddressResolveResult?> TryResolveAsync(
        GhnAddressResolveInput input,
        CancellationToken cancellationToken = default)
    {
        var wardName = input.Ward?.Trim();
        var districtName = input.District?.Trim();
        var cityName = input.City?.Trim();
        var provinceName = input.Province?.Trim();

        if (string.IsNullOrWhiteSpace(wardName))
            return null;

        var (provinceQuery, districtQuery) = InferProvinceAndDistrict(
            wardName, districtName, cityName, provinceName);

        if (string.IsNullOrWhiteSpace(provinceQuery))
            return null;

        try
        {
            var provinces = await _ghn.GetProvincesAsync(cancellationToken);
            var province = FindBestMatch(provinceQuery, provinces, p => p.ProvinceName, districtName ?? cityName);
            if (province == null)
            {
                _logger.LogDebug("[GHN resolve] Không khớp tỉnh: {City}/{Province}", cityName, provinceName);
                return null;
            }

            var districts = await _ghn.GetDistrictsAsync(province.ProvinceId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(districtQuery))
            {
                var district = FindBestMatch(districtQuery, districts, d => d.DistrictName, districtQuery);
                if (district != null)
                {
                    var ward = await FindWardInDistrictAsync(wardName, district.DistrictId, cancellationToken);
                    if (ward != null)
                        return LogAndReturn(wardName, districtQuery, cityName, district.DistrictId, ward.WardCode);
                }
            }

            // Fallback: quét phường trong toàn tỉnh (xử lý district/city lưu sai field).
            var scanned = await ScanWardInProvinceAsync(wardName, districts, cancellationToken);
            if (scanned.HasValue)
            {
                var hit = scanned.Value;
                return LogAndReturn(wardName, hit.DistrictName, cityName, hit.DistrictId, hit.WardCode);
            }

            _logger.LogDebug(
                "[GHN resolve] Không khớp {Ward}/{District}/{City} tại {Province}",
                wardName, districtQuery, cityName, province.ProvinceName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GHN resolve] Lỗi khi map địa chỉ");
            return null;
        }
    }

    private static (string? Province, string? District) InferProvinceAndDistrict(
        string ward,
        string? district,
        string? city,
        string? province)
    {
        var districtLooksLikeProvince = LooksLikeProvince(district);
        var cityLooksLikeProvince = LooksLikeProvince(city);
        var cityLooksLikeDistrict = LooksLikeDistrict(city);
        var districtLooksLikeDistrict = LooksLikeDistrict(district);

        string? provinceQuery = null;
        string? districtQuery = null;

        if (cityLooksLikeProvince)
            provinceQuery = city;
        else if (districtLooksLikeProvince)
            provinceQuery = district;
        else if (!string.IsNullOrWhiteSpace(city) && !IsCountryOnly(city))
            provinceQuery = city;
        else if (!string.IsNullOrWhiteSpace(province) && !IsCountryOnly(province))
            provinceQuery = province;

        if (districtLooksLikeDistrict)
            districtQuery = district;
        else if (cityLooksLikeDistrict)
            districtQuery = city;

        // Ghép từ addressLine kiểu "..., Phường X, Thành phố Y, Tỉnh Z" khi district trống.
        if (string.IsNullOrWhiteSpace(districtQuery) && !string.IsNullOrWhiteSpace(district)
            && !districtLooksLikeProvince)
            districtQuery = district;

        return (provinceQuery, districtQuery);
    }

    private static bool LooksLikeProvince(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || IsCountryOnly(value))
            return false;

        var n = Normalize(value);
        if (n is "ho chi minh" or "ha noi" or "da nang" or "can tho" or "hai phong")
            return true;

        return Regex.IsMatch(value, @"(tỉnh|thành phố|tp\.?\s)", RegexOptions.IgnoreCase)
               && !Regex.IsMatch(value, @"(quận|huyện|phường|xã)", RegexOptions.IgnoreCase);
    }

    private static bool LooksLikeDistrict(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || IsCountryOnly(value))
            return false;

        return Regex.IsMatch(value, @"(quận|huyện|thành phố|thị xã|tp\.?\s)", RegexOptions.IgnoreCase)
               || Normalize(value) is "thu duc" or "quan 1" or "quan 2" or "quan 3" or "quan 9";
    }

    private async Task<GhnWardDto?> FindWardInDistrictAsync(
        string wardName,
        int districtId,
        CancellationToken cancellationToken)
    {
        var wards = await _ghn.GetWardsAsync(districtId, cancellationToken);
        return FindBestMatch(wardName, wards, w => w.WardName, wardName);
    }

    private async Task<(int DistrictId, string DistrictName, string WardCode)?> ScanWardInProvinceAsync(
        string wardName,
        IReadOnlyList<GhnDistrictDto> districts,
        CancellationToken cancellationToken)
    {
        (int DistrictId, string DistrictName, string WardCode, int Score)? best = null;

        foreach (var district in districts)
        {
            var ward = await FindWardInDistrictAsync(wardName, district.DistrictId, cancellationToken);
            if (ward == null)
                continue;

            var score = ScoreMatch(Normalize(wardName), Normalize(ward.WardName));
            if (best == null || score > best.Value.Score)
                best = (district.DistrictId, district.DistrictName, ward.WardCode, score);
        }

        return best is { Score: >= 80 } b
            ? (b.DistrictId, b.DistrictName, b.WardCode)
            : null;
    }

    private GhnAddressResolveResult LogAndReturn(
        string wardName,
        string? districtName,
        string? cityName,
        int districtId,
        string wardCode)
    {
        _logger.LogInformation(
            "[GHN resolve] {Ward}/{District}/{City} → district_id={DistrictId}, ward_code={WardCode}",
            wardName, districtName, cityName, districtId, wardCode);
        return new GhnAddressResolveResult(districtId, wardCode);
    }

    private static bool IsCountryOnly(string value)
    {
        var n = Normalize(value);
        return n is "viet nam" or "vn" or "vietnam";
    }

    private static T? FindBestMatch<T>(
        string query,
        IReadOnlyList<T> items,
        Func<T, string> nameSelector,
        string? originalHint = null)
    {
        if (items.Count == 0)
            return default;

        var normalizedQuery = Normalize(query);
        if (string.IsNullOrEmpty(normalizedQuery))
            return default;

        var hint = originalHint ?? query;
        var hintNorm = Normalize(hint);
        var hintHasThanhPho = ContainsThanhPho(hint);

        T? best = default;
        var bestScore = 0;

        foreach (var item in items)
        {
            var rawName = nameSelector(item);
            var score = ScoreMatch(normalizedQuery, Normalize(rawName));
            score += TieBreakBonus(rawName, hintNorm, hintHasThanhPho);
            if (score > bestScore)
            {
                bestScore = score;
                best = item;
            }
        }

        return bestScore >= 80 ? best : default;
    }

    private static int TieBreakBonus(string candidateRaw, string hintNorm, bool hintHasThanhPho)
    {
        var bonus = 0;
        var candidateNorm = Normalize(candidateRaw);

        if (hintHasThanhPho && ContainsThanhPho(candidateRaw))
            bonus += 5;
        if (!hintHasThanhPho && candidateRaw.Contains("Quận", StringComparison.OrdinalIgnoreCase)
            && hintNorm.Contains("quan", StringComparison.Ordinal))
            bonus += 3;
        if (candidateNorm == hintNorm)
            bonus += 2;

        return bonus;
    }

    private static bool ContainsThanhPho(string value) =>
        value.Contains("Thành phố", StringComparison.OrdinalIgnoreCase)
        || value.Contains("Thành Phố", StringComparison.OrdinalIgnoreCase);

    private static int ScoreMatch(string query, string candidate)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(candidate))
            return 0;
        if (query == candidate)
            return 100;
        if (candidate.Contains(query) || query.Contains(candidate))
            return 90;

        var queryTokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var candidateTokens = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (queryTokens.Length == 0 || candidateTokens.Length == 0)
            return 0;

        var overlap = queryTokens.Count(t => candidateTokens.Contains(t));
        return overlap * 100 / Math.Max(queryTokens.Length, candidateTokens.Length);
    }

    internal static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var s = RemoveDiacritics(input.Trim().ToLowerInvariant());
        s = PrefixRegex.Replace(s, string.Empty);
        s = Regex.Replace(s, @"[^a-z0-9\s]", " ");
        s = Regex.Replace(s, @"\s+", " ").Trim();

        s = s switch
        {
            "ho chi minh" or "hcm" or "tp hcm" or "sai gon" or "tp ho chi minh" => "ho chi minh",
            "ha noi" or "hn" or "tp ha noi" => "ha noi",
            "da nang" or "tp da nang" => "da nang",
            "thu duc" or "tp thu duc" or "thanh pho thu duc" => "thu duc",
            _ => s
        };

        return s;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
