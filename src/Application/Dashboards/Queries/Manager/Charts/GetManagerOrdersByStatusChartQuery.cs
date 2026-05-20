using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerOrdersByStatusChartQuery : IRequest<DashboardChartSeriesDTO>;

public class GetManagerOrdersByStatusChartQueryHandler : IRequestHandler<GetManagerOrdersByStatusChartQuery, DashboardChartSeriesDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerOrdersByStatusChartQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardChartSeriesDTO> Handle(GetManagerOrdersByStatusChartQuery request, CancellationToken ct)
    {
        var counts = await DashboardStatusHelper.CountByStatusAsync(_context.Orders, x => x.OrderStatus, ct);
        var points = DashboardStatusHelper.Merge(OrderStatuses.All, counts)
            .Select(x => new DashboardChartPointDTO { Key = x.Status, Label = x.Label, Value = x.Count, Count = x.Count })
            .ToList();

        return DashboardChartFactory.Create("orders-by-status", "Đơn hàng theo trạng thái", points);
    }
}
