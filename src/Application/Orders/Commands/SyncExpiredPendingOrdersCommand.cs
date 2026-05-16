using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record SyncExpiredPendingOrdersCommand : IRequest<SyncExpiredPendingOrdersResultDTO>;

public record SyncExpiredPendingOrdersResultDTO(int CancelledCount);

public class SyncExpiredPendingOrdersCommandHandler : IRequestHandler<SyncExpiredPendingOrdersCommand, SyncExpiredPendingOrdersResultDTO>
{
    private readonly IOrderPendingService _orderPendingService;

    public SyncExpiredPendingOrdersCommandHandler(IOrderPendingService orderPendingService)
    {
        _orderPendingService = orderPendingService;
    }

    public async Task<SyncExpiredPendingOrdersResultDTO> Handle(SyncExpiredPendingOrdersCommand request, CancellationToken cancellationToken)
    {
        var cancelledCount = await _orderPendingService.CancelExpiredPendingOrdersAsync(cancellationToken);
        return new SyncExpiredPendingOrdersResultDTO(cancelledCount);
    }
}
