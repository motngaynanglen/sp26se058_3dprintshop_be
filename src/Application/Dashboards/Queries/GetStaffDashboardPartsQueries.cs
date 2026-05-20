using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record GetStaffDashboardSummaryQuery : IRequest<StaffDashboardSummaryDTO>;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record GetStaffDashboardWorkQueueQuery : IRequest<StaffDashboardWorkQueueDTO>;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record GetStaffDashboardRecentWorkQuery : IRequest<StaffDashboardRecentWorkDTO>;

internal static class StaffDashboardScope
{
    public static async Task<Staff?> GetStaffAsync(IApplicationDbContext context, IUser user, CancellationToken ct)
    {
        var userId = user.Id.ToGuid();
        return await context.Staffs
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.AccountId == userId, ct);
    }

    public static IQueryable<Order> Orders(IApplicationDbContext context, IUser user, Staff? staff)
    {
        return user.Role == Roles.STAFF
            ? context.Orders.Where(x => staff != null && x.StaffId == staff.Id)
            : context.Orders.Where(x => x.OrderStatus == OrderStatuses.Processing || x.OrderStatus == OrderStatuses.Finished);
    }

    public static IQueryable<DesignWork> DesignWorks(IApplicationDbContext context, IUser user, Staff? staff)
    {
        return user.Role == Roles.STAFF
            ? context.DesignWorks.Where(x => staff != null && x.MainAssignedStaffId == staff.Id)
            : context.DesignWorks.Where(x => x.Status != DesignWorkStatus.Completed);
    }

    public static void EnsureStaffScope(IUser user, Staff? staff)
    {
        if (user.Role == Roles.STAFF && staff == null)
            throw new ForbiddenAccessException("Không tìm thấy hồ sơ nhân viên của tài khoản hiện tại.");
    }
}

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
