using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerShipmentsByStatusChartQuery : IRequest<DashboardChartSeriesDTO>;

public class GetManagerShipmentsByStatusChartQueryHandler : IRequestHandler<GetManagerShipmentsByStatusChartQuery, DashboardChartSeriesDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerShipmentsByStatusChartQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardChartSeriesDTO> Handle(GetManagerShipmentsByStatusChartQuery request, CancellationToken ct)
    {
        var counts = await DashboardStatusHelper.CountByStatusAsync(_context.Shipments, x => x.ShipmentStatus, ct);
        var points = DashboardStatusHelper.Merge(ShipmentStatuses.All, counts)
            .Select(x => new DashboardChartPointDTO { Key = x.Status, Label = x.Label, Value = x.Count, Count = x.Count })
            .ToList();

        return DashboardChartFactory.Create("shipments-by-status", "Vận đơn theo trạng thái", points);
    }
}
