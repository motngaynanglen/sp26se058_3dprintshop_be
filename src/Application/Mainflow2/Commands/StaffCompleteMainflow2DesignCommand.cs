using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

/// <summary>NV gửi bảng thiết kế sau cọc — mở thanh toán phần còn lại cho khách.</summary>
public record StaffCompleteMainflow2DesignCommand : IRequest<Unit>
{
    public Guid DesignWorkId { get; init; }
    public string DeliverableFileUrl { get; init; } = string.Empty;
    public string? Note { get; init; }
}

public class StaffCompleteMainflow2DesignCommandValidator : AbstractValidator<StaffCompleteMainflow2DesignCommand>
{
    public StaffCompleteMainflow2DesignCommandValidator()
    {
        RuleFor(x => x.DesignWorkId).NotEmpty();
        RuleFor(x => x.DeliverableFileUrl).NotEmpty().WithMessage("Cần file bảng thiết kế (GLB).");
    }
}

public class StaffCompleteMainflow2DesignCommandHandler : IRequestHandler<StaffCompleteMainflow2DesignCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public StaffCompleteMainflow2DesignCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Unit> Handle(StaffCompleteMainflow2DesignCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var role = _user.Role ?? Roles.GUEST;
        if (role != Roles.STAFF && role != Roles.MANAGER && role != Roles.ADMIN)
            throw new UnauthorizedAccessException("Chỉ nhân viên mới gửi bảng thiết kế.");

        var dw = await _context.DesignWorks
            .FirstOrDefaultAsync(d => d.Id == request.DesignWorkId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");

        if (!Mainflow2DesignAccess.IsMainflow2(dw))
            throw new InvalidOperationException("Yêu cầu không thuộc luồng custom.");

        if (!await Mainflow2DesignAccess.CanStaffViewAsync(_context, accountId, role, dw, cancellationToken))
            throw new UnauthorizedAccessException("Không có quyền xử lý yêu cầu này.");

        var linkedOrder = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.DesignWorkId == dw.Id && oi.Order.OrderStatus != OrderStatuses.Cancelled)
            .OrderByDescending(oi => oi.Created)
            .Select(oi => new { oi.Order.Invoice!.PaymentStatus })
            .FirstOrDefaultAsync(cancellationToken);

        if (linkedOrder?.PaymentStatus != InvoiceStatuses.PartiallyPaid)
            throw new InvalidOperationException("Chỉ gửi bảng thiết kế sau khi khách đã đặt cọc 30%.");

        if (await Mainflow2DesignFlowHelper.IsDesignReadyForBalanceAsync(_context, dw.Id, cancellationToken))
            return Unit.Value;

        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? "staff";
        var deliverableUrl = request.DeliverableFileUrl.Trim();

        await Mainflow2DesignFlowHelper.MarkDesignReadyAsync(
            _context,
            dw.Id,
            accountId,
            username,
            deliverableUrl,
            request.Note,
            now,
            cancellationToken);

        dw.LastModified = now;
        dw.LastModifiedBy = username;
        await _context.SaveChangesAsync(cancellationToken);

        await _realtime.NotifyAsync(dw.Id, "design_deliverable", new
        {
            dw.Id,
            deliverableFileUrl = deliverableUrl,
        }, cancellationToken);

        return Unit.Value;
    }
}
