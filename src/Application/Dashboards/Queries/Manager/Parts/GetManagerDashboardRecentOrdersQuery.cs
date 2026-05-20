using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardRecentOrdersQuery : IRequest<ManagerDashboardRecentOrdersDTO>;

public class GetManagerDashboardRecentOrdersQueryHandler : IRequestHandler<GetManagerDashboardRecentOrdersQuery, ManagerDashboardRecentOrdersDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardRecentOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ManagerDashboardRecentOrdersDTO> Handle(GetManagerDashboardRecentOrdersQuery request, CancellationToken ct)
    {
        return new ManagerDashboardRecentOrdersDTO
        {
            GeneratedAt = CoreHelper.SystemTimeNow,
            RecentOrders = await _context.Orders
                .Include(x => x.Customer)
                    .ThenInclude(x => x.Account)
                .OrderByDescending(x => x.Created)
                .Take(10)
                .Select(x => new DashboardRecentOrderDTO
                {
                    Id = x.Id,
                    Code = x.Code,
                    CustomerName = x.Customer.Account.Fullname,
                    TotalPrice = x.TotalPrice,
                    OrderStatus = x.OrderStatus,
                    Created = x.Created
                })
                .ToListAsync(ct)
        };
    }
}
