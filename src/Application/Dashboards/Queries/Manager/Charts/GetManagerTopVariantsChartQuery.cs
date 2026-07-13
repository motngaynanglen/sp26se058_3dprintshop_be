using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerTopVariantsChartQuery : IRequest<DashboardChartSeriesDTO>
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Limit { get; init; } = 10;
}

public class GetManagerTopVariantsChartQueryHandler : IRequestHandler<GetManagerTopVariantsChartQuery, DashboardChartSeriesDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerTopVariantsChartQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardChartSeriesDTO> Handle(GetManagerTopVariantsChartQuery request, CancellationToken ct)
    {
        var limit = request.Limit is < 1 or > 50 ? 10 : request.Limit;
        var query = _context.OrderItems
            .Where(x => x.DesignVariantId != null);

        if (request.From.HasValue)
            query = query.Where(x => x.Order.Created.UtcDateTime >= request.From.Value.Date);
        if (request.To.HasValue)
            query = query.Where(x => x.Order.Created.UtcDateTime < request.To.Value.Date.AddDays(1));

        // Gom nhóm + Sum trong SQL, format Key (Guid→string) ở memory.
        var raw = await query
            .GroupBy(x => new { x.DesignVariantId, x.DesignVariant!.Code, x.DesignVariant.Name })
            .Select(g => new
            {
                g.Key.DesignVariantId,
                g.Key.Code,
                g.Key.Name,
                Qty = g.Sum(i => i.QuantityOrdered)
            })
            .OrderByDescending(r => r.Qty)
            .Take(limit)
            .ToListAsync(ct);

        var points = raw
            .Select(r => new DashboardChartPointDTO
            {
                Key = r.DesignVariantId!.Value.ToString(),
                Label = (r.Code ?? string.Empty) + " - " + (r.Name ?? string.Empty),
                Value = r.Qty,
                Count = r.Qty
            })
            .ToList();

        return new DashboardChartSeriesDTO
        {
            ChartKey = "top-variants",
            Title = "Top sản phẩm/biến thể bán chạy",
            GeneratedAt = CoreHelper.SystemTimeNow,
            From = request.From,
            To = request.To,
            Points = points
        };
    }
}
