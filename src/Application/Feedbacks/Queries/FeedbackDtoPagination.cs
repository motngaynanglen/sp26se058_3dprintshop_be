using Microsoft.EntityFrameworkCore;
using AutoMapper;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;

/// <summary>
/// Feedback → FeedbackDTO dùng MaskName C# và collection ImageUrls — không dùng ProjectTo (không dịch được SQL).
/// </summary>
internal static class FeedbackDtoPagination
{
    public static async Task<PaginatedList<FeedbackDTO>> ToPaginatedListAsync(
        this IQueryable<Feedback> query,
        IMapper mapper,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        query = query
            .AsNoTracking()
            .Include(f => f.FeedbackImages)
            .Include(f => f.DesignTemplate)
            .Include(f => f.Customer).ThenInclude(c => c.Account)
            .Include(f => f.OrderItem).ThenInclude(oi => oi.DesignVariant)
            .Include(f => f.OrderItem).ThenInclude(oi => oi.DesignWork!)
                .ThenInclude(dw => dw.MainAssignedStaff!)
                    .ThenInclude(s => s.Account)
            .Include(f => f.OrderItem).ThenInclude(oi => oi.Order)
                .ThenInclude(o => o.Staff!)
                    .ThenInclude(s => s.Account);

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(f => f.Created)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = mapper.Map<List<FeedbackDTO>>(items);
        return new PaginatedList<FeedbackDTO>(dtos, count, pageNumber, pageSize);
    }
}
