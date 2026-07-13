using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

/// <summary>
/// Closes (locks) a Quick Print DesignWork that has NOT yet created an Order.
/// This is the "withdraw" action for the free-service phase — before CheckoutDraftsCommand is called.
/// Different from CancelOrderCommand which requires an Order to exist.
/// The work remains visible in history (IsLocked = true), but no further uploads or reviews are possible.
/// </summary>
[Authorize(Roles = Roles.CustomerStaffManager)]
public record CloseQuickPrintRequestCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid DesignWorkId { get; init; }

    [DefaultValue(null)]
    public string? Reason { get; init; }
}

public class CloseQuickPrintRequestCommandValidator : AbstractValidator<CloseQuickPrintRequestCommand>
{
    public CloseQuickPrintRequestCommandValidator()
    {
        RuleFor(x => x.DesignWorkId)
            .NotEmpty().WithMessage("ID yêu cầu in không hợp lệ.");

        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Lý do không được quá 1000 ký tự.");
    }
}

public class CloseQuickPrintRequestCommandHandler : IRequestHandler<CloseQuickPrintRequestCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ILogger<CloseQuickPrintRequestCommandHandler> _logger;

    public CloseQuickPrintRequestCommandHandler(
        IApplicationDbContext context,
        IUser user,
        ILogger<CloseQuickPrintRequestCommandHandler> logger)
    {
        _context = context;
        _user = user;
        _logger = logger;
    }

    public async Task<bool> Handle(CloseQuickPrintRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid();
        var isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;

        var designWork = await _context.DesignWorks
            .FirstOrDefaultAsync(dw => dw.Id == request.DesignWorkId, cancellationToken);

        if (designWork == null)
            throw new DataNotFoundException(nameof(DesignWork), request.DesignWorkId);

        // Only Quick Print works can be closed with this command
        if (designWork.WorkType != DesignWorkTypes.PrintService)
            throw new BusinessException(
                "Lệnh này chỉ áp dụng cho yêu cầu in nhanh (PRINT_SERVICE). " +
                "Đối với đơn thiết kế, hãy dùng chức năng hủy đơn hàng.",
                ResponseCodeConstants.VAL_INVALID_STATE);

        // Cannot close a completed/locked work
        if (designWork.IsLocked)
            throw new BusinessException(
                "Yêu cầu in đã hoàn tất và bị khóa. Không thể đóng.",
                ResponseCodeConstants.VAL_INVALID_STATE);

        // Ownership: customer can only close their own work; staff/manager can close any
        if (!isStaffOrManager)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AccountId == userId, cancellationToken);

            if (customer == null || designWork.CustomerId != customer.Id)
                throw new ForbiddenAccessException("Bạn không có quyền đóng yêu cầu in này.");
        }

        // Guard: no active Order must exist for this DesignWork
        // (If CheckoutDraftsCommand was already called, an Order exists — use CancelOrderCommand instead)
        var hasActiveOrder = await _context.OrderItems
            .AnyAsync(item =>
                item.DesignWorkId == request.DesignWorkId &&
                item.Order != null &&
                item.Order.OrderStatus != OrderStatuses.Cancelled,
                cancellationToken);

        if (hasActiveOrder)
            throw new BusinessException(
                "Yêu cầu in đã có đơn hàng liên kết. Vui lòng hủy đơn hàng thay vì đóng yêu cầu in.",
                ResponseCodeConstants.VAL_INVALID_STATE);

        var now = CoreHelper.SystemTimeNow;
        var actor = _user.Username ?? "SYSTEM";
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Không có lý do." : request.Reason.Trim();

        // Create a System log for audit trail — work stays visible in history
        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = request.DesignWorkId,
            AccountId = isStaffOrManager ? (userId == Guid.Empty ? null : userId) : userId,
            Content = $"Yêu cầu in đã được đóng bởi {(isStaffOrManager ? "nhân viên" : "khách hàng")}. Lý do: {reason}",
            LogType = DesignLogType.System,
            Created = now,
            CreatedBy = actor,
            LastModified = now,
            LastModifiedBy = actor
        });

        // Lock the work — preserves full history, prevents further file uploads/reviews
        designWork.IsLocked = true;
        designWork.LastModified = now;
        designWork.LastModifiedBy = actor;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "QuickPrint DesignWork {WorkId} closed (locked) by {Actor}. Reason: {Reason}",
            request.DesignWorkId, actor, reason);

        return true;
    }
}
