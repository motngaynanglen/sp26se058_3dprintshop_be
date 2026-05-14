using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using sp26se058_3dprintshop_be.Application.DesignLogs.Commands;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;

namespace sp26se058_3dprintshop_be.Web.Hubs;

[Authorize]
public class DesignWorkChatHub : Hub
{
    public const string Route = "/hubs/design-work-chat";
    public const string ReceiveDesignLogEvent = "ReceiveDesignLog";
    public const string JoinedDesignWorkEvent = "JoinedDesignWork";
    public const string LeftDesignWorkEvent = "LeftDesignWork";

    private readonly ISender _sender;
    private readonly ILogger<DesignWorkChatHub> _logger;

    public DesignWorkChatHub(ISender sender, ILogger<DesignWorkChatHub> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task JoinDesignWork(Guid designWorkId)
    {
        if (designWorkId == Guid.Empty)
        {
            throw new HubException("DesignWorkId is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(designWorkId));
        await Clients.Caller.SendAsync(JoinedDesignWorkEvent, designWorkId);
    }

    public async Task LeaveDesignWork(Guid designWorkId)
    {
        if (designWorkId == Guid.Empty)
        {
            throw new HubException("DesignWorkId is required.");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(designWorkId));
        await Clients.Caller.SendAsync(LeftDesignWorkEvent, designWorkId);
    }

    public async Task<DesignLogDTO> SendMessage(CreateDesignLogCommand request)
    {
        if (request.DesignWorkId == Guid.Empty)
        {
            throw new HubException("DesignWorkId is required.");
        }

        var result = await _sender.Send(request);

        await Clients
            .Group(GetGroupName(result.DesignWorkId))
            .SendAsync(ReceiveDesignLogEvent, result);

        _logger.LogInformation(
            "Design work chat message {DesignLogId} was sent to design work {DesignWorkId}.",
            result.Id,
            result.DesignWorkId);

        return result;
    }

    public static string GetGroupName(Guid designWorkId) => $"design-work-{designWorkId:N}";
}
