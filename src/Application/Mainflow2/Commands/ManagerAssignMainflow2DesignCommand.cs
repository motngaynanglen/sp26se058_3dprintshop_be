using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

/// <summary>Manager/Admin chỉ định nhân viên xử lý yêu cầu Mainflow2.</summary>
public record ManagerAssignMainflow2DesignCommand : IRequest<Unit>
{
    public Guid DesignWorkId { get; init; }
    public Guid StaffId { get; init; }
}

public class ManagerAssignMainflow2DesignCommandHandler : IRequestHandler<ManagerAssignMainflow2DesignCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public ManagerAssignMainflow2DesignCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Unit> Handle(ManagerAssignMainflow2DesignCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        if (!Mainflow2DesignAccess.IsManager(_user.Role))
            throw new UnauthorizedAccessException("Chỉ quản lý mới giao việc cho nhân viên.");

        var accountId = _user.Id.ToGuid();
        var dw = await _context.DesignWorks.FirstOrDefaultAsync(d => d.Id == request.DesignWorkId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");

        if (!Mainflow2DesignAccess.IsMainflow2(dw))
            throw new InvalidOperationException("Yêu cầu không thuộc Mainflow 2.");

        if (dw.Status is Mainflow2DesignWorkStatuses.Cancelled or Mainflow2DesignWorkStatuses.Approved)
            throw new InvalidOperationException("Không thể giao việc ở trạng thái hiện tại.");

        var staff = await _context.Staffs
            .Include(s => s.Account)
            .FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken)
            ?? throw new InvalidOperationException("Nhân viên không tồn tại.");

        if (!staff.Account.IsActive)
            throw new InvalidOperationException("Nhân viên đã bị vô hiệu hóa.");

        dw.MainAssignedStaffId = staff.Id;
        dw.StaffAssignedAt = CoreHelper.SystemTimeNow;
        if (dw.Status == Mainflow2DesignWorkStatuses.Submitted)
            dw.Status = Mainflow2DesignWorkStatuses.Assigned;
        dw.LastModified = CoreHelper.SystemTimeNow;
        dw.LastModifiedBy = _user.Username ?? "manager";

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = $"Manager giao việc cho {staff.Account.Fullname ?? staff.Account.Username}",
            LogType = Mainflow2DesignLogTypes.StatusChange,
            IsAI = false,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "manager",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "manager"
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyAsync(dw.Id, "assigned", new
        {
            designWorkId = dw.Id,
            mainAssignedStaffId = dw.MainAssignedStaffId,
            status = dw.Status
        }, cancellationToken);

        return Unit.Value;
    }
}
