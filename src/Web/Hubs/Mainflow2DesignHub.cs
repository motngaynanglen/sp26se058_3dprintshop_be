using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;

namespace sp26se058_3dprintshop_be.Web.Hubs;

[Authorize]
public sealed class Mainflow2DesignHub : Hub
{
    public const string GroupPrefix = "mf2-design-";

    private readonly IServiceScopeFactory _scopeFactory;

    public Mainflow2DesignHub(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task JoinDesignWork(string designWorkId)
    {
        if (!Guid.TryParse(designWorkId, out var id))
            throw new HubException("designWorkId không hợp lệ.");

        var accountIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
            throw new HubException("Chưa xác thực.");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);

        var dw = await db.DesignWorks.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (dw == null)
            throw new HubException("Không tìm thấy yêu cầu.");

        if (!Mainflow2DesignAccess.IsMainflow2(dw))
            throw new HubException("Không phải yêu cầu Mainflow 2.");

        var canCustomer = await Mainflow2DesignAccess.CanCustomerViewAsync(db, accountId, dw, Context.ConnectionAborted);
        var canStaff = await Mainflow2DesignAccess.CanStaffViewAsync(db, accountId, role, dw, Context.ConnectionAborted);
        if (!canCustomer && !canStaff)
            throw new HubException("Không có quyền tham gia kênh này.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupPrefix + id);
    }

    public async Task LeaveDesignWork(string designWorkId)
    {
        if (!Guid.TryParse(designWorkId, out var id))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupPrefix + id);
    }
}
