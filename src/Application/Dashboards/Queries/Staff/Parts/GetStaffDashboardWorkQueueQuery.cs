using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record GetStaffDashboardWorkQueueQuery : IRequest<StaffDashboardWorkQueueDTO>;

public class GetStaffDashboardWorkQueueQueryHandler : IRequestHandler<GetStaffDashboardWorkQueueQuery, StaffDashboardWorkQueueDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetStaffDashboardWorkQueueQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<StaffDashboardWorkQueueDTO> Handle(GetStaffDashboardWorkQueueQuery request, CancellationToken ct)
    {
        var staff = await StaffDashboardScope.GetStaffAsync(_context, _user, ct);
        StaffDashboardScope.EnsureStaffScope(_user, staff);

        var orders = StaffDashboardScope.Orders(_context, _user, staff);
        var designWorks = StaffDashboardScope.DesignWorks(_context, _user, staff);

        return new StaffDashboardWorkQueueDTO
        {
            GeneratedAt = CoreHelper.SystemTimeNow,
            StaffId = staff?.Id,
            StaffName = staff?.Account.Fullname ?? (_user.Role == Roles.MANAGER ? "Manager" : null),
            WorkQueue = await StaffDashboardWorkQueueBuilder.BuildAsync(_context, orders, designWorks, ct)
        };
    }
}
