using System.Security.Cryptography;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class CodeGeneratorService : ICodeGeneratorService
{
    private static readonly object LockObject = new();
    private static long _lastTimestamp;
    private static int _counter;

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
        var normalizedPrefix = NormalizePart(prefix);
        var normalizedScope = NormalizePart(scope);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyMMddHHmmssfff");
        var sequence = GetSequence(timestamp);
        var random = RandomNumberGenerator.GetInt32(0, 65536).ToString("X4");

        return string.IsNullOrWhiteSpace(normalizedScope)
            ? $"{normalizedPrefix}-{timestamp}{sequence:D3}-{random}"
            : $"{normalizedPrefix}-{normalizedScope}-{timestamp}{sequence:D3}-{random}";
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

    private static string NormalizePart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = value.Trim().ToUpperInvariant()
            .Where(c => char.IsLetterOrDigit(c))
            .Take(12)
            .ToArray();

        return new string(chars);
    }
}
