using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

public record AddMainflow2DesignMessageCommand : IRequest<Guid>
{
    public Guid DesignWorkId { get; init; }
    public required string Content { get; init; }
    public IReadOnlyList<string>? AttachmentUrls { get; init; }
}

public class AddMainflow2DesignMessageCommandValidator : AbstractValidator<AddMainflow2DesignMessageCommand>
{
    public AddMainflow2DesignMessageCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(8000);
    }
}

public class AddMainflow2DesignMessageCommandHandler : IRequestHandler<AddMainflow2DesignMessageCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public AddMainflow2DesignMessageCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Guid> Handle(AddMainflow2DesignMessageCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var dw = await _context.DesignWorks.FirstOrDefaultAsync(d => d.Id == request.DesignWorkId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");

        if (!Mainflow2DesignAccess.IsMainflow2(dw))
            throw new InvalidOperationException("Yêu cầu không thuộc Mainflow 2.");

        if (dw.Status is Mainflow2DesignWorkStatuses.Cancelled or Mainflow2DesignWorkStatuses.Approved)
            throw new InvalidOperationException("Không thể gửi tin nhắn ở trạng thái hiện tại.");

        var isCustomer = await _context.Customers.AnyAsync(c => c.AccountId == accountId, cancellationToken);
        var staff = await _context.Staffs.FirstOrDefaultAsync(s => s.AccountId == accountId, cancellationToken);

        if (!isCustomer && staff == null)
            throw new UnauthorizedAccessException("Không có quyền gửi tin nhắn.");

        if (isCustomer)
        {
            var customer = await _context.Customers.FirstAsync(c => c.AccountId == accountId, cancellationToken);
            if (dw.CustomerId != customer.Id)
                throw new UnauthorizedAccessException("Không có quyền với yêu cầu này.");
        }
        else if (staff != null)
        {
            if (dw.MainAssignedStaffId != staff.Id)
                throw new UnauthorizedAccessException("Chỉ nhân viên được phân công mới gửi tin nhắn.");
        }

        var logType = isCustomer ? Mainflow2DesignLogTypes.CustomerMessage : Mainflow2DesignLogTypes.StaffMessage;
        var meta = request.AttachmentUrls is { Count: > 0 }
            ? JsonSerializer.Serialize(request.AttachmentUrls)
            : null;

        if (isCustomer && dw.Status == Mainflow2DesignWorkStatuses.Quoted)
            dw.Status = Mainflow2DesignWorkStatuses.Negotiating;

        var log = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = request.Content.Trim(),
            Metadata = meta,
            LogType = logType,
            IsAI = false,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "user",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "user"
        };
        _context.DesignLogs.Add(log);

        dw.LastModified = CoreHelper.SystemTimeNow;
        dw.LastModifiedBy = _user.Username ?? "user";

        await _context.SaveChangesAsync(cancellationToken);

        await _realtime.NotifyAsync(dw.Id, "message", new
        {
            designWorkId = dw.Id,
            messageId = log.Id,
            logType = log.LogType,
            content = log.Content,
            metadataJson = log.Metadata,
            created = log.Created,
            isCustomer = isCustomer
        }, cancellationToken);

        return log.Id;
    }
}
