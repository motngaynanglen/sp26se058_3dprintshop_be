using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record GetStaffDashboardRecentWorkQuery : IRequest<StaffDashboardRecentWorkDTO>;

public class GetStaffDashboardRecentWorkQueryHandler : IRequestHandler<GetStaffDashboardRecentWorkQuery, StaffDashboardRecentWorkDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetStaffDashboardRecentWorkQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<StaffDashboardRecentWorkDTO> Handle(GetStaffDashboardRecentWorkQuery request, CancellationToken ct)
    {
        var staff = await StaffDashboardScope.GetStaffAsync(_context, _user, ct);
        StaffDashboardScope.EnsureStaffScope(_user, staff);

        var orders = StaffDashboardScope.Orders(_context, _user, staff);
        var designWorks = StaffDashboardScope.DesignWorks(_context, _user, staff);

        return new StaffDashboardRecentWorkDTO
        {
            GeneratedAt = CoreHelper.SystemTimeNow,
            StaffId = staff?.Id,
            StaffName = staff?.Account.Fullname ?? (_user.Role == Roles.MANAGER ? "Manager" : null),
            RecentAssignedOrders = await orders
                .Include(x => x.Customer)
                    .ThenInclude(x => x.Account)
                .OrderByDescending(x => x.LastModified)
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
                .ToListAsync(ct),
            RecentAssignedDesignWorks = await designWorks
                .Include(x => x.Customer)
                    .ThenInclude(x => x.Account)
                .OrderByDescending(x => x.LastModified)
                .Take(10)
                .Select(x => new DashboardDesignWorkDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Status = x.Status,
                    CustomerName = x.Customer.Account.Fullname,
                    Created = x.Created
                })
                .ToListAsync(ct)
        };
    }
}
