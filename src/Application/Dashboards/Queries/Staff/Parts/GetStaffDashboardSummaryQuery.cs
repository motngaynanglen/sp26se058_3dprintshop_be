using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record GetStaffDashboardSummaryQuery : IRequest<StaffDashboardSummaryDTO>;

public class GetStaffDashboardSummaryQueryHandler : IRequestHandler<GetStaffDashboardSummaryQuery, StaffDashboardSummaryDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetStaffDashboardSummaryQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<StaffDashboardSummaryDTO> Handle(GetStaffDashboardSummaryQuery request, CancellationToken ct)
    {
        var staff = await StaffDashboardScope.GetStaffAsync(_context, _user, ct);
        StaffDashboardScope.EnsureStaffScope(_user, staff);

        var orders = StaffDashboardScope.Orders(_context, _user, staff);
        var designWorks = StaffDashboardScope.DesignWorks(_context, _user, staff);

        return new StaffDashboardSummaryDTO
        {
            GeneratedAt = CoreHelper.SystemTimeNow,
            StaffId = staff?.Id,
            StaffName = staff?.Account.Fullname ?? (_user.Role == Roles.MANAGER ? "Manager" : null),
            AssignedOrdersByStatus = DashboardStatusHelper.Merge(OrderStatuses.All, await DashboardStatusHelper.CountByStatusAsync(orders, x => x.OrderStatus, ct)),
            AssignedDesignWorksByStatus = DashboardStatusHelper.Merge(DesignWorkStatus.All, await DashboardStatusHelper.CountByStatusAsync(designWorks, x => x.Status, ct)),
            ShipmentsByStatus = DashboardStatusHelper.Merge(ShipmentStatuses.All, await DashboardStatusHelper.CountByStatusAsync(_context.Shipments, x => x.ShipmentStatus, ct))
        };
    }
}
