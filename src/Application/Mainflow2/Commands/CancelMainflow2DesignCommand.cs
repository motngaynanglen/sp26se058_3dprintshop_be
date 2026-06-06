using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

public record CancelMainflow2DesignCommand : IRequest<Unit>
{
    public Guid DesignWorkId { get; init; }
}

public class CancelMainflow2DesignCommandHandler : IRequestHandler<CancelMainflow2DesignCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public CancelMainflow2DesignCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Unit> Handle(CancelMainflow2DesignCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var dw = await _context.DesignWorks.FirstOrDefaultAsync(d => d.Id == request.DesignWorkId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");

        if (!Mainflow2DesignAccess.IsMainflow2(dw))
            throw new InvalidOperationException("Yêu cầu không thuộc Mainflow 2.");

        if (dw.Status == Mainflow2DesignWorkStatuses.Cancelled)
            throw new InvalidOperationException("Yêu cầu đã được hủy trước đó.");

        if (dw.Status == Mainflow2DesignWorkStatuses.Approved)
        {
            var hasActiveOrder = await _context.OrderItems
                .AnyAsync(oi => oi.DesignWorkId == dw.Id && oi.Order.OrderStatus != OrderStatuses.Cancelled, cancellationToken);
            if (hasActiveOrder)
                throw new InvalidOperationException("Yêu cầu đã có đơn hàng — vui lòng hủy đơn hàng thay vì hủy yêu cầu.");
        }

        var isCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken);
        var staff = await _context.Staffs.FirstOrDefaultAsync(s => s.AccountId == accountId, cancellationToken);

        if (isCustomer != null)
        {
            if (dw.CustomerId != isCustomer.Id)
                throw new UnauthorizedAccessException("Không có quyền với yêu cầu này.");
        }
        else if (staff != null)
        {
            if (dw.MainAssignedStaffId != staff.Id && !Mainflow2DesignAccess.IsManager(_user.Role))
                throw new UnauthorizedAccessException("Không có quyền hủy yêu cầu này.");
        }
        else
            throw new UnauthorizedAccessException("Không có quyền.");

        dw.Status = Mainflow2DesignWorkStatuses.Cancelled;
        dw.LastModified = CoreHelper.SystemTimeNow;
        dw.LastModifiedBy = _user.Username ?? "user";

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = Mainflow2DesignWorkStatuses.Cancelled,
            LogType = Mainflow2DesignLogTypes.StatusChange,
            IsAI = false,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "user",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "user"
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyAsync(dw.Id, "cancelled", new { dw.Id, dw.Status }, cancellationToken);

        return Unit.Value;
    }
}
