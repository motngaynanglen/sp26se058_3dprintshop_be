using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2;

public static class Mainflow2DesignFlowHelper
{
    public static async Task<bool> IsDesignReadyForBalanceAsync(
        IApplicationDbContext context,
        Guid designWorkId,
        CancellationToken ct)
    {
        return await context.DesignLogs
            .AsNoTracking()
            .AnyAsync(l => l.DesignWorkId == designWorkId
                           && l.LogType == Mainflow2DesignLogTypes.DesignReady, ct);
    }

    public static Task AfterDepositPaidAsync(
        IApplicationDbContext context,
        Order order,
        CancellationToken ct) =>
        Task.CompletedTask;

    public static async Task MarkDesignReadyAsync(
        IApplicationDbContext context,
        Guid designWorkId,
        Guid actorAccountId,
        string username,
        string deliverableFileUrl,
        string? note,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (await IsDesignReadyForBalanceAsync(context, designWorkId, ct))
            return;

        var fileUrl = deliverableFileUrl.Trim();
        var metaJson = JsonSerializer.Serialize(new
        {
            deliverableFileUrl = fileUrl,
            deliverableFileUrls = new[] { fileUrl }
        });

        var logId = Guid.NewGuid();
        context.DesignLogs.Add(new DesignLog
        {
            Id = logId,
            DesignWorkId = designWorkId,
            AccountId = actorAccountId,
            LogType = Mainflow2DesignLogTypes.DesignReady,
            Content = string.IsNullOrWhiteSpace(note)
                ? "Bảng thiết kế đã sẵn sàng — vui lòng hoàn tất thanh toán để bắt đầu sản xuất."
                : note.Trim(),
            Metadata = metaJson,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username,
        });

        var maxVer = await context.DesignVersionHistorys
            .Where(v => v.DesignWorkId == designWorkId)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(ct) ?? 0;

        context.DesignVersionHistorys.Add(new DesignVersionHistory
        {
            Id = Guid.NewGuid(),
            DesignWorkId = designWorkId,
            DesignLogId = logId,
            UploaderId = actorAccountId,
            FileUrl = fileUrl,
            VersionNumber = maxVer + 1,
            Tilte = "Bảng thiết kế",
            IsPreviewable = true,
            IsApproved = false,
            IsPrintable = true,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username,
        });
    }

    public static async Task<bool> CanPayBalanceAsync(
        IApplicationDbContext context,
        Order order,
        CancellationToken ct)
    {
        if (order.Invoice == null || !OrderPaymentHelper.IsInvoicePartiallyPaid(order.Invoice))
            return false;

        if (OrderPaymentHelper.IsDirectPrintOrder(order))
            return false;

        var designWorkIds = order.OrderItems
            .Where(oi => oi.DesignWorkId != null && SourceTypes.IsCustomPrintFlow(oi.SourceType))
            .Select(oi => oi.DesignWorkId!.Value)
            .Distinct()
            .ToList();

        if (designWorkIds.Count == 0)
            return true;

        foreach (var id in designWorkIds)
        {
            if (!await IsDesignReadyForBalanceAsync(context, id, ct))
                return false;
        }

        return true;
    }
}
