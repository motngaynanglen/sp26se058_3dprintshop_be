using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignLogs.Commands;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Web.Hubs;

[Authorize(Roles = Roles.CustomerStaffManager)]
public class DesignWorkChatHub : Hub
{
    public const string Route = "/hubs/design-work-chat";
    public const string ReceiveDesignLogEvent = "ReceiveDesignLog";
    public const string JoinedDesignWorkEvent = "JoinedDesignWork";
    public const string LeftDesignWorkEvent = "LeftDesignWork";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DesignWorkChatHub> _logger;

    public DesignWorkChatHub(IServiceScopeFactory scopeFactory, ILogger<DesignWorkChatHub> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task JoinDesignWork(Guid designWorkId)
    {
        if (designWorkId == Guid.Empty)
            throw new HubException("DesignWorkId is required.");

        // Lấy AccountId và Role từ JWT claims
        var accountIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountIdStr) || !Guid.TryParse(accountIdStr, out var accountId))
            throw new HubException("Chưa xác thực.");

        var role = Context.User?.FindFirstValue(ClaimTypes.Role) ?? "";

        // Kiểm tra quyền truy cập DesignWork
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var dw = await db.DesignWorks.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == designWorkId, Context.ConnectionAborted);

        if (dw == null)
            throw new HubException("Không tìm thấy yêu cầu thiết kế.");

        // Manager: luôn có quyền
        // Customer: phải là chủ đơn (DesignWork.CustomerId → Customer.AccountId == accountId)
        // Staff: phải là staff được assign hoặc có role Manager
        var hasAccess = false;

        if (role == Roles.MANAGER)
        {
            hasAccess = true;
        }
        else if (role == Roles.CUSTOMER)
        {
            hasAccess = await db.Customers.AsNoTracking()
                .AnyAsync(c => c.Id == dw.CustomerId && c.AccountId == accountId,
                    Context.ConnectionAborted);
        }
        else if (role == Roles.STAFF)
        {
            // Staff được assign trực tiếp
            if (dw.MainAssignedStaffId.HasValue)
            {
                hasAccess = await db.Staffs.AsNoTracking()
                    .AnyAsync(s => s.Id == dw.MainAssignedStaffId.Value && s.AccountId == accountId,
                        Context.ConnectionAborted);
            }

            // Nếu chưa assign ai → cho phép staff join (để tiếp nhận)
            if (!hasAccess && !dw.MainAssignedStaffId.HasValue)
            {
                hasAccess = await db.Staffs.AsNoTracking()
                    .AnyAsync(s => s.AccountId == accountId, Context.ConnectionAborted);
            }
        }

        if (!hasAccess)
            throw new HubException("Không có quyền tham gia kênh này.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(designWorkId));
        await Clients.Caller.SendAsync(JoinedDesignWorkEvent, designWorkId);

        _logger.LogInformation(
            "User {AccountId} ({Role}) joined design work {DesignWorkId}.",
            accountId, role, designWorkId);
    }

    public async Task LeaveDesignWork(Guid designWorkId)
    {
        if (designWorkId == Guid.Empty)
            throw new HubException("DesignWorkId is required.");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(designWorkId));
        await Clients.Caller.SendAsync(LeftDesignWorkEvent, designWorkId);
    }

    /// <summary>
    /// Gửi tin nhắn trực tiếp qua hub (thay vì REST endpoint).
    /// REST endpoint (CreateChatLog) cũng broadcast qua IHubContext nên cả 2 cách đều realtime.
    /// </summary>
    public async Task<DesignLogDTO> SendMessage(CreateDesignLogCommand request)
    {
        if (request.DesignWorkId == Guid.Empty)
            throw new HubException("DesignWorkId is required.");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(request);

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
