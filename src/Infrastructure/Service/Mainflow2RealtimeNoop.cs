using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public sealed class Mainflow2RealtimeNoop : IMainflow2RealtimeNotifier
{
    public Task NotifyAsync(Guid designWorkId, string eventName, object payload, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
