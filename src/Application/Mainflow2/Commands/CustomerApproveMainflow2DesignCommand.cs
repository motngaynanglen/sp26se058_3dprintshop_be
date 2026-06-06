using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

public record CustomerApproveMainflow2DesignCommand : IRequest<Unit>
{
    public Guid DesignWorkId { get; init; }
}

public class CustomerApproveMainflow2DesignCommandHandler : IRequestHandler<CustomerApproveMainflow2DesignCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public CustomerApproveMainflow2DesignCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Unit> Handle(CustomerApproveMainflow2DesignCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        await Mainflow2DesignAccess.EnsureCustomerAsync(_context, accountId, cancellationToken);
        var customer = await _context.Customers.FirstAsync(c => c.AccountId == accountId, cancellationToken);

        var dw = await _context.DesignWorks.FirstOrDefaultAsync(d => d.Id == request.DesignWorkId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");

        if (!Mainflow2DesignAccess.IsMainflow2(dw))
            throw new InvalidOperationException("Yêu cầu không thuộc Mainflow 2.");

        if (dw.CustomerId != customer.Id)
            throw new UnauthorizedAccessException("Không có quyền với yêu cầu này.");

        if (dw.LatestQuotedPrice == null || dw.LastQuotedAt == null)
            throw new InvalidOperationException("Chưa có báo giá để chấp nhận.");

        if (dw.Status is Mainflow2DesignWorkStatuses.Cancelled or Mainflow2DesignWorkStatuses.Approved)
            throw new InvalidOperationException("Không thể chấp nhận ở trạng thái hiện tại.");

        dw.Status = Mainflow2DesignWorkStatuses.Approved;
        dw.CustomerApprovedAt = CoreHelper.SystemTimeNow;
        dw.LastModified = CoreHelper.SystemTimeNow;
        dw.LastModifiedBy = _user.Username ?? "customer";

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = Mainflow2DesignWorkStatuses.Approved,
            LogType = Mainflow2DesignLogTypes.StatusChange,
            IsAI = false,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "customer",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "customer"
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyAsync(dw.Id, "approved", new { dw.Id, dw.Status }, cancellationToken);

        return Unit.Value;
    }
}
