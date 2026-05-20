using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDesignWorksByStatusChartQuery : IRequest<DashboardChartSeriesDTO>;

public class GetManagerDesignWorksByStatusChartQueryHandler : IRequestHandler<GetManagerDesignWorksByStatusChartQuery, DashboardChartSeriesDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDesignWorksByStatusChartQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardChartSeriesDTO> Handle(GetManagerDesignWorksByStatusChartQuery request, CancellationToken ct)
    {
        var counts = await DashboardStatusHelper.CountByStatusAsync(_context.DesignWorks, x => x.Status, ct);
        var points = DashboardStatusHelper.Merge(DesignWorkStatus.All, counts)
            .Select(x => new DashboardChartPointDTO { Key = x.Status, Label = x.Label, Value = x.Count, Count = x.Count })
            .ToList();

        return DashboardChartFactory.Create("design-works-by-status", "Thiết kế theo trạng thái", points);
    }
}
