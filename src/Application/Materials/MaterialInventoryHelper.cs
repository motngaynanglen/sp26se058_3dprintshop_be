using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Materials;

public static class MaterialInventoryHelper
{
    public const decimal LowStockThresholdGrams = 50_000m;

    private static readonly HashSet<string> MaterialDeductionSourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        SourceTypes.PreOrder,
        SourceTypes.CustomFilePrintMainflow2,
        SourceTypes.CustomQuoteMainflow2,
    };

    public static async Task DeductForProductionStartAsync(
        IApplicationDbContext context,
        Order order,
        string username,
        DateTimeOffset now,
        CancellationToken ct)
    {
        foreach (var item in order.OrderItems)
        {
            if (!MaterialDeductionSourceTypes.Contains(item.SourceType))
                continue;

            if (item.FulfillmentStatus != OrderItemStatuses.Printing)
                continue;

            var alreadyDeducted = await context.MaterialInventoryTransactions
                .AnyAsync(t => t.ReferenceId == item.Id && t.Type == MaterialInventoryTransactionTypes.OrderOut, ct);
            if (alreadyDeducted)
                continue;

            var deductions = await ResolveMaterialGramsAsync(context, item, ct);
            if (deductions.Count == 0)
                continue;

            foreach (var (materialId, grams) in deductions)
            {
                if (grams <= 0)
                    continue;

                var material = await context.Materials
                    .FirstOrDefaultAsync(m => m.Id == materialId, ct);
                if (material == null)
                    continue;

                material.StockQuantityGrams -= grams;
                material.LastModified = now;
                material.LastModifiedBy = username;

                context.MaterialInventoryTransactions.Add(new MaterialInventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    MaterialId = materialId,
                    Type = MaterialInventoryTransactionTypes.OrderOut,
                    QuantityGrams = -grams,
                    ReferenceId = item.Id,
                    Note = $"Xuất {grams:N0}g cho đơn {order.Code} — {item.ItemName ?? item.SourceType}",
                    Created = now,
                    CreatedBy = username,
                    LastModified = now,
                    LastModifiedBy = username
                });
            }
        }
    }

    public static async Task<List<(Guid MaterialId, decimal Grams)>> ResolveMaterialGramsAsync(
        IApplicationDbContext context,
        OrderItem item,
        CancellationToken ct)
    {
        if (item.SourceType == SourceTypes.PreOrder && item.DesignVariantId.HasValue)
        {
            var variant = await context.DesignVariants
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == item.DesignVariantId, ct);

            if (variant?.EstimatedWeightPerUnit is > 0)
            {
                var grams = variant.EstimatedWeightPerUnit.Value * item.QuantityOrdered;
                return [(variant.MaterialId, grams)];
            }

            return [];
        }

        if (item.DesignWorkId.HasValue
            && (item.SourceType == SourceTypes.CustomFilePrintMainflow2
                || item.SourceType == SourceTypes.CustomQuoteMainflow2))
        {
            var metadata = await context.DesignLogs
                .AsNoTracking()
                .Where(l => l.DesignWorkId == item.DesignWorkId
                            && l.LogType == Mainflow2DesignLogTypes.StaffQuote)
                .OrderByDescending(l => l.Created)
                .Select(l => l.Metadata)
                .FirstOrDefaultAsync(ct);

            return ParseGramsFromQuoteMetadata(metadata, item.QuantityOrdered);
        }

        return [];
    }

    internal static List<(Guid MaterialId, decimal Grams)> ParseGramsFromQuoteMetadata(
        string? metadataJson,
        int orderQuantity)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || orderQuantity <= 0)
            return [];

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (!doc.RootElement.TryGetProperty("quoteBreakdown", out var breakdownElement))
                return [];

            var breakdown = JsonSerializer.Deserialize<Mainflow2QuoteBreakdownDto>(
                breakdownElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (breakdown?.Components == null)
                return [];

            var totals = new Dictionary<Guid, decimal>();
            foreach (var component in breakdown.Components)
            {
                foreach (var line in component.Materials)
                {
                    if (line.MaterialId == Guid.Empty || line.TotalGrams <= 0)
                        continue;

                    totals[line.MaterialId] = totals.GetValueOrDefault(line.MaterialId) + line.TotalGrams;
                }
            }

            return totals
                .Select(kv => (kv.Key, kv.Value * orderQuantity))
                .Where(x => x.Item2 > 0)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
