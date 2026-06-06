using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Mainflow2;

public static class Mainflow2QuoteFeeHelper
{
    public static decimal? TryParseDesignFeeFromQuoteMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;
            if (TryReadDecimal(root, "laborCost", out var topLevel))
                return Math.Max(0, topLevel);

            if (root.TryGetProperty("quoteBreakdown", out var breakdown)
                && TryReadDecimal(breakdown, "laborCost", out var nested))
                return Math.Max(0, nested);
        }
        catch
        {
            // metadata không phải JSON hợp lệ
        }

        return null;
    }

    public static decimal? TryParseMaterialSubtotalFromQuoteMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;
            if (TryReadDecimal(root, "materialSubtotal", out var topLevel))
                return Math.Max(0, topLevel);

            if (root.TryGetProperty("quoteBreakdown", out var breakdown)
                && TryReadDecimal(breakdown, "materialSubtotal", out var nested))
                return Math.Max(0, nested);
        }
        catch
        {
            // bỏ qua
        }

        return null;
    }

    public static async Task<decimal> ResolveDesignFeeForDesignWorkAsync(
        IApplicationDbContext context,
        Guid designWorkId,
        CancellationToken ct)
    {
        var metadata = await context.DesignLogs
            .AsNoTracking()
            .Where(l => l.DesignWorkId == designWorkId && l.LogType == Mainflow2DesignLogTypes.StaffQuote)
            .OrderByDescending(l => l.Created)
            .Select(l => l.Metadata)
            .FirstOrDefaultAsync(ct);

        return TryParseDesignFeeFromQuoteMetadata(metadata) ?? 0;
    }

    public static async Task<decimal> ResolveDesignFeeForOrderAsync(
        IApplicationDbContext context,
        Order order,
        CancellationToken ct)
    {
        var designWorkIds = order.OrderItems
            .Where(oi => oi.DesignWorkId != null
                         && (SourceTypes.IsCustomPrintFlow(oi.SourceType)
                             || oi.SourceType == SourceTypes.CustomQuoteMainflow2))
            .Select(oi => oi.DesignWorkId!.Value)
            .Distinct()
            .ToList();

        if (designWorkIds.Count == 0)
            return 0;

        decimal total = 0;
        foreach (var id in designWorkIds)
            total += await ResolveDesignFeeForDesignWorkAsync(context, id, ct);

        return total;
    }

    private static bool TryReadDecimal(JsonElement element, string propertyName, out decimal value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var prop))
            return false;

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(prop.GetString(), out value),
            _ => false
        };
    }
}
