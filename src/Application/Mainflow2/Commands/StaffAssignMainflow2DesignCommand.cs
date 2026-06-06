using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

public record StaffAssignMainflow2DesignCommand : IRequest<Unit>
{
    public Guid DesignWorkId { get; init; }
}

public class StaffAssignMainflow2DesignCommandHandler : IRequestHandler<StaffAssignMainflow2DesignCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public StaffAssignMainflow2DesignCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Unit> Handle(StaffAssignMainflow2DesignCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var staff = await Mainflow2DesignAccess.EnsureStaffAsync(_context, accountId, cancellationToken);

        var dw = await _context.DesignWorks.FirstOrDefaultAsync(d => d.Id == request.DesignWorkId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");

        if (!Mainflow2DesignAccess.IsMainflow2(dw))
            throw new InvalidOperationException("Yêu cầu không thuộc Mainflow 2.");

        if (dw.Status is Mainflow2DesignWorkStatuses.Cancelled or Mainflow2DesignWorkStatuses.Approved)
            throw new InvalidOperationException("Không thể tiếp nhận ở trạng thái hiện tại.");

        if (dw.MainAssignedStaffId != null && dw.MainAssignedStaffId != staff.Id)
            throw new InvalidOperationException("Yêu cầu đã được nhân viên khác tiếp nhận.");

        dw.MainAssignedStaffId = staff.Id;
        dw.StaffAssignedAt = CoreHelper.SystemTimeNow;
        if (dw.Status == Mainflow2DesignWorkStatuses.Submitted)
            dw.Status = Mainflow2DesignWorkStatuses.Assigned;
        dw.LastModified = CoreHelper.SystemTimeNow;
        dw.LastModifiedBy = _user.Username ?? "staff";

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = Mainflow2DesignWorkStatuses.Assigned,
            LogType = Mainflow2DesignLogTypes.StatusChange,
            IsAI = false,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "staff",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "staff"
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
