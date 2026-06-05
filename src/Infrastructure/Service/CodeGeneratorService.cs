using System.Security.Cryptography;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

/// <summary>
/// Tạo mã đơn hàng / hóa đơn.
/// PayOS yêu cầu orderCode tối đa 25 ký tự.
///
/// Format: {PREFIX}{SCOPE}-{TIMESTAMP}{SEQ}{RND}
/// VD: ORDIS-0605123456001A3 (23 ký tự, ≤ 25)
///
/// Breakdown:
///   PREFIX: 3 chars (ORD / INV)
///   SCOPE:  2 chars (IS=InStock, PO=PreOrder, DS=DesignService, PS=PrintService, blank)
///   -:      1 char
///   TIMESTAMP: 10 chars (MMddHHmmss)
///   SEQ:    3 chars (000-999)
///   RND:    2 chars (hex)
///   Total max: 3 + 2 + 1 + 10 + 3 + 2 = 21 (có scope) hoặc 19 (không scope)
/// </summary>
public class CodeGeneratorService : ICodeGeneratorService
{
    private static readonly object LockObject = new();
    private static long _lastTimestamp;
    private static int _counter;

    // Map scope dài → 2 ký tự
    private static readonly Dictionary<string, string> ScopeShortMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "IN_STOCK", "IS" },
        { "INSTOCK", "IS" },
        { "PRE_ORDER", "PO" },
        { "PREORDER", "PO" },
        { "DESIGN_SERVICE", "DS" },
        { "DESIGNSERVICE", "DS" },
        { "PRINT_SERVICE", "PS" },
        { "PRINTSERVICE", "PS" },
    };

    public string GenerateOrderCode(string? scope = null)
    {
        return GenerateCode("ORD", scope);
    }

    public string GenerateInvoiceCode(string? scope = null)
    {
        return GenerateCode("INV", scope);
    }

    public string GenerateCode(string prefix, string? scope = null)
    {
        var shortScope = ShortenScope(scope);
        var timestamp = DateTimeOffset.UtcNow.ToString("MMddHHmmss"); // 10 chars
        var seq = GetSequence(timestamp).ToString("D3");               // 3 chars
        var rnd = RandomNumberGenerator.GetInt32(0, 256).ToString("X2"); // 2 chars hex

        // VD: ORDIS-0605123456001A3  (21 chars max)
        return string.IsNullOrEmpty(shortScope)
            ? $"{prefix}-{timestamp}{seq}{rnd}"    // 3+1+10+3+2 = 19
            : $"{prefix}{shortScope}-{timestamp}{seq}{rnd}"; // 3+2+1+10+3+2 = 21
    }

    private static string ShortenScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope)) return string.Empty;
        var trimmed = scope.Trim();
        if (ScopeShortMap.TryGetValue(trimmed, out var shortCode)) return shortCode;
        // Fallback: lấy 2 ký tự đầu
        var clean = new string(trimmed.Where(char.IsLetterOrDigit).Take(2).ToArray()).ToUpperInvariant();
        return clean;
    }

    private static int GetSequence(string timestampText)
    {
        var timestamp = long.Parse(timestampText);
        lock (LockObject)
        {
            if (timestamp == _lastTimestamp)
            {
                _counter++;
            }
            else
            {
                _lastTimestamp = timestamp;
                _counter = 0;
            }

            return _counter % 1000;
        }
    }
}
