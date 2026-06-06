using Microsoft.AspNetCore.SignalR;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Web.Hubs;

namespace sp26se058_3dprintshop_be.Web.Services;

public sealed class Mainflow2RealtimeNotifier : IMainflow2RealtimeNotifier
{
    private readonly IHubContext<Mainflow2DesignHub> _hubContext;

    public Mainflow2RealtimeNotifier(IHubContext<Mainflow2DesignHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyAsync(Guid designWorkId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.Group(Mainflow2DesignHub.GroupPrefix + designWorkId)
            .SendAsync("Mainflow2Event", new { eventName, payload }, cancellationToken);
    }
}
