using sp26se058_3dprintshop_be.Application.Common.Extensions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.Feedbacks;

public static class FeedbackCustomerHelper
{
    public static async Task<Guid> GetCurrentCustomerIdAsync(
        IApplicationDbContext context,
        IUser user,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = user.Id.ToGuid();
        var customerId = await context.Customers
            .AsNoTracking()
            .Where(c => c.AccountId == accountId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (customerId is null)
            throw new UnauthorizedAccessException("Chỉ khách hàng mới thực hiện được thao tác này.");

        return customerId.Value;
    }
}
