using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Mainflow2;

public static class Mainflow2DesignAccess
{
    /// <summary>Luồng custom có chat + báo giá KTV: MF2 mô tả, file STL, mô hình AI, in từ thiết kế, in lại.</summary>
    public static bool IsMainflow2(DesignWork dw) =>
        SourceTypes.IsCustomPrintFlow(dw.SourceType)
        || dw.SourceType == SourceTypes.CustomQuoteMainflow2;

    public static async Task EnsureCustomerAsync(IApplicationDbContext context, Guid accountId, CancellationToken ct)
    {
        if (!await context.Customers.AnyAsync(c => c.AccountId == accountId, ct))
            throw new UnauthorizedAccessException("Chỉ khách hàng mới thực hiện được thao tác này.");
    }

    public static async Task<Staff> EnsureStaffAsync(IApplicationDbContext context, Guid accountId, CancellationToken ct)
    {
        var staff = await context.Staffs.FirstOrDefaultAsync(s => s.AccountId == accountId, ct);
        if (staff == null)
            throw new UnauthorizedAccessException("Chỉ nhân viên mới thực hiện được thao tác này.");
        return staff;
    }

    public static bool IsManager(string? role) =>
        role is Roles.MANAGER or Roles.ADMIN;

    public static async Task<bool> CanCustomerViewAsync(IApplicationDbContext context, Guid accountId, DesignWork dw, CancellationToken ct)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.AccountId == accountId, ct);
        return customer != null && dw.CustomerId == customer.Id;
    }

    public static async Task<bool> CanStaffViewAsync(IApplicationDbContext context, Guid accountId, string? role, DesignWork dw, CancellationToken ct)
    {
        if (IsManager(role))
            return true;

        var staff = await context.Staffs.FirstOrDefaultAsync(s => s.AccountId == accountId, ct);
        if (staff == null)
            return false;

        return dw.MainAssignedStaffId == staff.Id || dw.MainAssignedStaffId == null;
    }
}
