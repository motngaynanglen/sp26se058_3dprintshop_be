namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

public interface IOrderPendingService
{
    Task EnsureCustomerHasNoPendingOrderAsync(Guid customerId, CancellationToken cancellationToken);
    Task<int> CancelExpiredPendingOrdersAsync(CancellationToken cancellationToken);
}
