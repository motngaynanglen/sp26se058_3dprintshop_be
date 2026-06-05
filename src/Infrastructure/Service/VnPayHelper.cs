using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

/// <summary>
/// HMAC-SHA512 + urlencode theo tài liệu VNPay paymentv2 (mẫu PHP / VnPayLibrary C#).
/// </summary>
internal static class VnPayHelper
{
    public static string BuildSignData(IEnumerable<KeyValuePair<string, string>> parameters)
        => BuildPaymentQueryString(FilterSignParameters(parameters));

    public static string HmacSha512(string key, string signData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(signData);
        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    public static string BuildPaymentUrl(string paymentBaseUrl, string hashSecret, IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var query = BuildPaymentQueryString(parameters);
        var hash = HmacSha512(hashSecret.Trim(), query);
        return $"{paymentBaseUrl.TrimEnd('?')}?{query}&vnp_SecureHash={hash}";
    }

    private static IEnumerable<KeyValuePair<string, string>> FilterSignParameters(
        IEnumerable<KeyValuePair<string, string>> parameters)
        => parameters.Where(p =>
            !string.IsNullOrEmpty(p.Value)
            && !p.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
            && !p.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase));

    public static string BuildPaymentQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var sorted = FilterSignParameters(parameters)
            .OrderBy(p => p.Key, VnPayKeyComparer.Instance);

        var sb = new StringBuilder();
        foreach (var kv in sorted)
        {
            sb.Append(VnPayUrlEncode(kv.Key));
            sb.Append('=');
            sb.Append(VnPayUrlEncode(kv.Value));
            sb.Append('&');
        }

        if (sb.Length > 0)
            sb.Length--;

        return sb.ToString();
    }

    /// <summary>Giống PHP urlencode: space → '+', hex chữ thường.</summary>
    public static string VnPayUrlEncode(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length * 2);
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '*')
            {
                sb.Append(c);
                continue;
            }

            if (c == ' ')
            {
                sb.Append('+');
                continue;
            }

            // PHP urlencode dùng %XX chữ HOA — VNPay gateway verify theo chuẩn PHP
            foreach (var b in Encoding.UTF8.GetBytes(new[] { c }))
                sb.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    public static Dictionary<string, string> ParseQueryToDictionary(IReadOnlyDictionary<string, string> source)
    {
        return source.ToDictionary(
            kv => kv.Key,
            kv => kv.Value ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Định dạng theo giờ Việt Nam (GMT+7) — VNPay bắt buộc, không dùng timezone máy chủ.</summary>
    public static string FormatVnPayDate(DateTimeOffset dt)
        => CoreHelper.UtcToOffsetSystemTime(dt).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

    public static string SanitizeOrderInfo(string orderCode)
        => $"Thanh toan don hang {orderCode}".Replace(':', ' ').Trim();

    private sealed class VnPayKeyComparer : IComparer<string>
    {
        public static readonly VnPayKeyComparer Instance = new();
        private static readonly CompareInfo EnUsOrdinal = CompareInfo.GetCompareInfo("en-US");

        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            return EnUsOrdinal.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}
