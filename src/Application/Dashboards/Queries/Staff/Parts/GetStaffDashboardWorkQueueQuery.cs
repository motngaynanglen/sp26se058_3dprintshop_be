using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
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
            WorkQueue =
            [
                new() { Key = "assigned-processing-orders", Label = "Đơn đang xử lý", Count = await orders.CountAsync(x => x.OrderStatus == OrderStatuses.Processing, ct), Severity = "INFO" },
                new() { Key = "assigned-finished-orders", Label = "Đơn chờ giao/hoàn tất", Count = await orders.CountAsync(x => x.OrderStatus == OrderStatuses.Finished, ct), Severity = "INFO" },
                new() { Key = "assigned-design-in-progress", Label = "Thiết kế đang thực hiện", Count = await designWorks.CountAsync(x => x.Status == DesignWorkStatus.InProgress, ct), Severity = "INFO" },
                new() { Key = "assigned-design-reviewing", Label = "Thiết kế chờ duyệt", Count = await designWorks.CountAsync(x => x.Status == DesignWorkStatus.Reviewing, ct), Severity = "WARNING" },
                new() { Key = "pending-address-change-requests", Label = "Yêu cầu đổi địa chỉ chờ xử lý", Count = await _context.ShipmentAddressChangeRequests.CountAsync(x => x.Status == ShipmentAddressChangeRequestStatuses.Pending, ct), Severity = "WARNING" },
                new() { Key = "shipments-preparing", Label = "Vận đơn đang đóng gói", Count = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Preparing, ct), Severity = "INFO" },
                new() { Key = "shipments-ready", Label = "Vận đơn chờ lấy hàng", Count = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.ReadyForPickup, ct), Severity = "INFO" },
                new() { Key = "shipments-failed", Label = "Vận đơn giao thất bại", Count = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Failed, ct), Severity = "WARNING" },
                new() { Key = "shipments-returning", Label = "Vận đơn đang hoàn hàng", Count = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Returning, ct), Severity = "WARNING" }
            ]
        };
    }
}
