using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Orders;

public static class OrderFeedbackRules
{
    /// <summary>Đơn đã hoàn tất — khách được gửi feedback nếu dòng hàng chưa có.</summary>
    public static bool IsOrderReviewable(
        string? orderStatus,
        string? shipmentStatus,
        DateTimeOffset? completedAt = null)
    {
        if (completedAt.HasValue)
            return true;

        if (string.Equals(orderStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(shipmentStatus, "DELIVERED", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<Guid> ResolveDesignTemplateIdAsync(
        IApplicationDbContext context,
        OrderItem item,
        CancellationToken cancellationToken)
    {
        if (item.DesignVariant?.DesignTemplateId is { } variantTemplateId)
            return variantTemplateId;

        if (item.DesignVariantId is { } designVariantId)
        {
            var fromVariant = await context.DesignVariants
                .AsNoTracking()
                .Where(dv => dv.Id == designVariantId)
                .Select(dv => (Guid?)dv.DesignTemplateId)
                .FirstOrDefaultAsync(cancellationToken);
            if (fromVariant.HasValue)
                return fromVariant.Value;
        }

        if (item.DesignWork?.TemplateId is { } workTemplateId)
            return workTemplateId;

        if (item.DesignWorkId is { } designWorkId)
        {
            var fromWork = await context.DesignWorks
                .AsNoTracking()
                .Where(dw => dw.Id == designWorkId)
                .Select(dw => dw.TemplateId)
                .FirstOrDefaultAsync(cancellationToken);
            if (fromWork.HasValue)
                return fromWork.Value;
        }

        var fromSibling = await context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.OrderId == item.OrderId && oi.DesignVariantId != null)
            .Join(
                context.DesignVariants.AsNoTracking(),
                oi => oi.DesignVariantId,
                dv => dv.Id,
                (_, dv) => dv.DesignTemplateId)
            .FirstOrDefaultAsync(cancellationToken);
        if (fromSibling != Guid.Empty)
            return fromSibling;

        var fallback = await context.DesignTemplates
            .AsNoTracking()
            .OrderBy(t => t.Created)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (fallback != Guid.Empty)
            return fallback;

        throw new InvalidOperationException("Không xác định được mẫu thiết kế để lưu đánh giá.");
    }
}
