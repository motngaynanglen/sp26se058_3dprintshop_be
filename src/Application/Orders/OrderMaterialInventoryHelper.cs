using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Materials;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Orders;

public static class OrderMaterialInventoryHelper
{
    public static async Task DeductMaterialAfterPaymentAsync(
        IApplicationDbContext context,
        Order order,
        string username,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await MaterialInventoryHelper.DeductForProductionStartAsync(context, order, username, now, ct);
    }
}
