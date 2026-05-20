using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;
using System.Linq.Expressions;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record GetStaffDashboardQuery : IRequest<StaffDashboardDTO>;

public class GetStaffDashboardQueryHandler : IRequestHandler<GetStaffDashboardQuery, StaffDashboardDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetStaffDashboardQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<StaffDashboardDTO> Handle(GetStaffDashboardQuery request, CancellationToken ct)
    {
        var now = CoreHelper.SystemTimeNow;
        var userId = _user.Id.ToGuid();
        var staff = await _context.Staffs
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.AccountId == userId, ct);

        var isStaff = _user.Role == Roles.STAFF;
        if (isStaff && staff == null)
            throw new ForbiddenAccessException("Không tìm thấy hồ sơ nhân viên của tài khoản hiện tại.");

        var assignedOrdersQuery = isStaff
            ? _context.Orders.Where(x => x.StaffId == staff!.Id)
            : _context.Orders.Where(x => x.OrderStatus == OrderStatuses.Processing || x.OrderStatus == OrderStatuses.Finished);

        var assignedDesignWorksQuery = isStaff
            ? _context.DesignWorks.Where(x => x.MainAssignedStaffId == staff!.Id)
            : _context.DesignWorks.Where(x => x.Status != DesignWorkStatus.Completed);

        var assignedOrderCounts = await CountByStatusAsync(assignedOrdersQuery, x => x.OrderStatus, ct);
        var assignedDesignWorkCounts = await CountByStatusAsync(assignedDesignWorksQuery, x => x.Status, ct);
        var shipmentCounts = await CountByStatusAsync(_context.Shipments, x => x.ShipmentStatus, ct);

        var assignedProcessingOrders = await assignedOrdersQuery.CountAsync(x => x.OrderStatus == OrderStatuses.Processing, ct);
        var assignedFinishedOrders = await assignedOrdersQuery.CountAsync(x => x.OrderStatus == OrderStatuses.Finished, ct);
        var assignedDesignInProgress = await assignedDesignWorksQuery.CountAsync(x => x.Status == DesignWorkStatus.InProgress, ct);
        var assignedDesignReviewing = await assignedDesignWorksQuery.CountAsync(x => x.Status == DesignWorkStatus.Reviewing, ct);
        var pendingAddressChangeRequests = await _context.ShipmentAddressChangeRequests
            .CountAsync(x => x.Status == ShipmentAddressChangeRequestStatuses.Pending, ct);
        var shipmentsPreparing = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Preparing, ct);
        var shipmentsReady = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.ReadyForPickup, ct);
        var shipmentsFailed = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Failed, ct);
        var shipmentsReturning = await _context.Shipments.CountAsync(x => x.ShipmentStatus == ShipmentStatuses.Returning, ct);

        var recentAssignedOrders = await assignedOrdersQuery
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
            .ToListAsync(ct);

        var recentAssignedDesignWorks = await assignedDesignWorksQuery
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
            .ToListAsync(ct);

        var workQueue = new List<DashboardActionItemDTO>
        {
            new() { Key = "assigned-processing-orders", Label = "Đơn đang xử lý", Count = assignedProcessingOrders, Severity = "INFO" },
            new() { Key = "assigned-finished-orders", Label = "Đơn chờ giao/hoàn tất", Count = assignedFinishedOrders, Severity = "INFO" },
            new() { Key = "assigned-design-in-progress", Label = "Thiết kế đang thực hiện", Count = assignedDesignInProgress, Severity = "INFO" },
            new() { Key = "assigned-design-reviewing", Label = "Thiết kế chờ duyệt", Count = assignedDesignReviewing, Severity = "WARNING" },
            new() { Key = "pending-address-change-requests", Label = "Yêu cầu đổi địa chỉ chờ xử lý", Count = pendingAddressChangeRequests, Severity = "WARNING" },
            new() { Key = "shipments-preparing", Label = "Vận đơn đang đóng gói", Count = shipmentsPreparing, Severity = "INFO" },
            new() { Key = "shipments-ready", Label = "Vận đơn chờ lấy hàng", Count = shipmentsReady, Severity = "INFO" },
            new() { Key = "shipments-failed", Label = "Vận đơn giao thất bại", Count = shipmentsFailed, Severity = "WARNING" },
            new() { Key = "shipments-returning", Label = "Vận đơn đang hoàn hàng", Count = shipmentsReturning, Severity = "WARNING" }
        };

        return new StaffDashboardDTO
        {
            GeneratedAt = now,
            StaffId = staff?.Id,
            StaffName = staff?.Account.Fullname ?? (_user.Role == Roles.MANAGER ? "Manager" : null),
            AssignedOrdersByStatus = DashboardStatusHelper.Merge(OrderStatuses.All, assignedOrderCounts),
            AssignedDesignWorksByStatus = DashboardStatusHelper.Merge(DesignWorkStatus.All, assignedDesignWorkCounts),
            ShipmentsByStatus = DashboardStatusHelper.Merge(ShipmentStatuses.All, shipmentCounts),
            WorkQueue = workQueue,
            RecentAssignedOrders = recentAssignedOrders,
            RecentAssignedDesignWorks = recentAssignedDesignWorks
        };
    }

    private static async Task<IReadOnlyCollection<DashboardStatusCountDTO>> CountByStatusAsync<TEntity>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, string>> statusSelector,
        CancellationToken ct)
    {
        return await query
            .GroupBy(statusSelector)
            .Select(x => new DashboardStatusCountDTO
            {
                Status = x.Key,
                Label = string.Empty,
                Count = x.Count()
            })
            .ToListAsync(ct);
    }
}
