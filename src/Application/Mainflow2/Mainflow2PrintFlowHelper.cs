using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2;

public static class Mainflow2PrintFlowHelper
{
    public static bool IsPrintableFile(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var lower = url.Trim().ToLowerInvariant();
        return lower.EndsWith(".stl") || lower.EndsWith(".obj") || lower.EndsWith(".glb");
    }

    public static bool IsPreviewableFile(string url) => IsPrintableFile(url);

    /// <summary>URL file/mô hình/ảnh để hiển thị trên danh sách in lại / in từ thiết kế.</summary>
    public static async Task<string?> ResolveListPreviewFileUrlAsync(
        IApplicationDbContext context,
        DesignWork dw,
        CancellationToken ct)
    {
        var versions = await context.DesignVersionHistorys
            .AsNoTracking()
            .Where(v => v.DesignWorkId == dw.Id)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        if (string.Equals(dw.SourceType, SourceTypes.AiGenerated, StringComparison.Ordinal))
        {
            var modelUrl = versions
                .OrderBy(v => v.VersionNumber)
                .Select(v => v.FileUrl)
                .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
            return modelUrl ?? dw.BaseImageUrl;
        }

        if (string.Equals(dw.SourceType, SourceTypes.CustomQuoteMainflow2, StringComparison.Ordinal))
        {
            var glb = versions
                .FirstOrDefault(v => v.IsPreviewable || IsPrintableFile(v.FileUrl))
                ?.FileUrl;
            if (!string.IsNullOrWhiteSpace(glb)) return glb;
            if (!string.IsNullOrWhiteSpace(dw.BaseImageUrl)) return dw.BaseImageUrl;
            return versions.FirstOrDefault()?.FileUrl;
        }

        if (string.Equals(dw.SourceType, SourceTypes.CustomFilePrintMainflow2, StringComparison.Ordinal))
            return dw.BaseImageUrl ?? versions.FirstOrDefault(v => v.IsPrintable)?.FileUrl;

        if (!string.IsNullOrWhiteSpace(dw.BaseImageUrl))
            return dw.BaseImageUrl;

        return versions.FirstOrDefault(v => v.IsPrintable || IsPrintableFile(v.FileUrl))?.FileUrl
               ?? versions.FirstOrDefault()?.FileUrl;
    }

    public static async Task<List<DesignVersionHistory>> LoadPrintableVersionsAsync(
        IApplicationDbContext context,
        Guid designWorkId,
        CancellationToken ct)
    {
        return await context.DesignVersionHistorys
            .AsNoTracking()
            .Where(v => v.DesignWorkId == designWorkId && v.IsPrintable)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);
    }

    public static async Task<bool> HasPaidOrderAsync(
        IApplicationDbContext context,
        Guid designWorkId,
        CancellationToken ct)
    {
        return await context.OrderItems
            .AsNoTracking()
            .AnyAsync(oi =>
                oi.DesignWorkId == designWorkId
                && oi.Order.OrderStatus != OrderStatuses.Cancelled
                && oi.Order.Invoice != null
                && (oi.Order.Invoice.PaymentStatus == InvoiceStatuses.Paid
                    || oi.Order.Invoice.PaymentStatus == InvoiceStatuses.PartiallyPaid),
                ct);
    }

    public static async Task<bool> HasActivePrintOrderAsync(
        IApplicationDbContext context,
        Guid designWorkId,
        CancellationToken ct)
    {
        return await context.OrderItems
            .AsNoTracking()
            .AnyAsync(oi =>
                oi.DesignWorkId == designWorkId
                && oi.Order.OrderStatus != OrderStatuses.Cancelled
                && oi.Order.OrderStatus != OrderStatuses.Completed,
                ct);
    }

    public static void CopyVersionHistories(
        IApplicationDbContext context,
        DesignWork target,
        IReadOnlyList<DesignVersionHistory> sources,
        Guid uploaderAccountId,
        string username,
        DateTimeOffset now)
    {
        var version = 1;
        foreach (var src in sources.OrderBy(v => v.VersionNumber))
        {
            context.DesignVersionHistorys.Add(new DesignVersionHistory
            {
                Id = Guid.NewGuid(),
                DesignWorkId = target.Id,
                UploaderId = uploaderAccountId,
                FileUrl = src.FileUrl,
                VersionNumber = version++,
                Tilte = src.Tilte ?? "File in",
                IsPreviewable = src.IsPreviewable || IsPreviewableFile(src.FileUrl),
                IsApproved = src.IsApproved,
                IsPrintable = true,
                Created = now,
                CreatedBy = username,
                LastModified = now,
                LastModifiedBy = username
            });
        }
    }

    public static DesignLog? FindLatestQuoteLog(IEnumerable<DesignLog> logs) =>
        logs
            .Where(l => l.LogType == Mainflow2DesignLogTypes.StaffQuote)
            .OrderByDescending(l => l.Created)
            .FirstOrDefault();

    public static void ApplyCopiedQuote(
        DesignWork target,
        DesignLog? quoteLog,
        DateTimeOffset now,
        string username,
        bool autoApprove)
    {
        if (quoteLog?.Metadata != null)
        {
            TryReadQuotedPriceFromMetadata(quoteLog.Metadata, target);
        }

        if (autoApprove && target.LatestQuotedPrice is > 0)
        {
            ApproveDesignWork(target, now, username, target.LastQuotedAt ?? now);
        }
    }

    /// <summary>Giá in/đơn vị từ báo giá, DesignWork nguồn hoặc đơn đã thanh toán.</summary>
    public static async Task<decimal?> ResolvePrintUnitPriceAsync(
        IApplicationDbContext context,
        DesignWork source,
        DesignLog? quoteLog,
        CancellationToken ct)
    {
        if (quoteLog?.Metadata != null)
        {
            var fromMeta = TryParseQuotedPrice(quoteLog.Metadata);
            if (fromMeta is > 0) return fromMeta;
        }

        if (source.LatestQuotedPrice is > 0)
            return source.LatestQuotedPrice;

        return await context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.DesignWorkId == source.Id
                         && oi.Order.OrderStatus != OrderStatuses.Cancelled)
            .OrderByDescending(oi => oi.Created)
            .Select(oi => (decimal?)oi.UnitPrice)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>TH2/TH3 — gán giá + APPROVED để checkout Pre-Order ngay.</summary>
    public static void FinalizeDirectPrintDesignWork(
        DesignWork dw,
        decimal? unitPrice,
        DesignWork source,
        DateTimeOffset now,
        string username)
    {
        if (unitPrice is > 0)
            dw.LatestQuotedPrice = unitPrice;

        if (dw.LatestQuotedPrice is > 0)
            ApproveDesignWork(dw, now, username, source.LastQuotedAt ?? now);
    }

    private static void TryReadQuotedPriceFromMetadata(string metadata, DesignWork target)
    {
        var price = TryParseQuotedPrice(metadata);
        if (price is > 0)
            target.LatestQuotedPrice = price;

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            if (doc.RootElement.TryGetProperty("revision", out var revEl) && revEl.TryGetInt32(out var rev))
                target.QuoteRevision = rev;
        }
        catch
        {
            /* bỏ qua */
        }
    }

    private static decimal? TryParseQuotedPrice(string metadata)
    {
        try
        {
            using var doc = JsonDocument.Parse(metadata);
            var root = doc.RootElement;
            if (root.TryGetProperty("quotedPrice", out var priceEl) && priceEl.TryGetDecimal(out var price) && price > 0)
                return price;
            if (root.TryGetProperty("QuotedPrice", out var priceEl2) && priceEl2.TryGetDecimal(out var price2) && price2 > 0)
                return price2;
        }
        catch
        {
            /* metadata không hợp lệ */
        }

        return null;
    }

    private static void ApproveDesignWork(
        DesignWork dw,
        DateTimeOffset now,
        string username,
        DateTimeOffset? lastQuotedAt)
    {
        dw.Status = Mainflow2DesignWorkStatuses.Approved;
        dw.CustomerApprovedAt = now;
        dw.LastQuotedAt = lastQuotedAt ?? now;
        dw.LastModified = now;
        dw.LastModifiedBy = username;
    }

    public static string BuildPrintBriefNote(
        string? printPriority,
        int? quantity,
        string? printSize,
        string? note)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(printPriority))
            parts.Add($"Ưu tiên in: {printPriority.Trim()}");
        if (quantity is > 0)
            parts.Add($"Số lượng: {quantity}");
        if (!string.IsNullOrWhiteSpace(printSize))
            parts.Add($"Kích thước: {printSize.Trim()}");
        if (!string.IsNullOrWhiteSpace(note))
            parts.Add(note.Trim());
        return parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
    }
}
