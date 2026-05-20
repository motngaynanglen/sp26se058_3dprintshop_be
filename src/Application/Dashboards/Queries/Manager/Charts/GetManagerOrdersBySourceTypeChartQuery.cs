using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerOrdersBySourceTypeChartQuery : IRequest<DashboardChartSeriesDTO>
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public class GetManagerOrdersBySourceTypeChartQueryHandler : IRequestHandler<GetManagerOrdersBySourceTypeChartQuery, DashboardChartSeriesDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerOrdersBySourceTypeChartQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardChartSeriesDTO> Handle(GetManagerOrdersBySourceTypeChartQuery request, CancellationToken ct)
    {
        var query = _context.OrderItems.AsQueryable();
        if (request.From.HasValue)
            query = query.Where(x => x.Order.Created.UtcDateTime >= request.From.Value.Date);
        if (request.To.HasValue)
            query = query.Where(x => x.Order.Created.UtcDateTime < request.To.Value.Date.AddDays(1));

        var rawPoints = await query
            .GroupBy(x => x.SourceType)
            .Select(x => new DashboardChartPointDTO
            {
                Key = x.Key,
                Label = x.Key,
                Value = x.Sum(i => i.TotalPrice),
                Count = x.Count()
            })
            .ToListAsync(ct);

        var labels = SourceTypes.All.ToDictionary(x => x.Value, x => x.Label, StringComparer.OrdinalIgnoreCase);
        var points = rawPoints
            .Select(x => new DashboardChartPointDTO
            {
                Key = x.Key,
                Label = labels.TryGetValue(x.Key, out var label) ? label : x.Label,
                Value = x.Value,
                Count = x.Count
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        return new DashboardChartSeriesDTO
        {
            ChartKey = "orders-by-source-type",
            Title = "Đơn hàng theo loại nguồn",
            GeneratedAt = CoreHelper.SystemTimeNow,
            From = request.From,
            To = request.To,
            Points = points
        };
    }
}
