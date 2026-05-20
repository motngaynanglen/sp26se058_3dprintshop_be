using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

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
