using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignLogs.Commands;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record ReviewAdjustmentRequestLogCommand : IRequest<DesignLogDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }

    [DefaultValue(true)]
    public bool IsApproved { get; init; }

    [DefaultValue("Yêu cầu nằm trong phạm vi brief ban đầu.")]
    public string? DecisionNote { get; init; }
}

public class ReviewAdjustmentRequestLogCommandValidator : AbstractValidator<ReviewAdjustmentRequestLogCommand>
{
    public ReviewAdjustmentRequestLogCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.DecisionNote)
            .MaximumLength(1000);

        RuleFor(x => x.DecisionNote)
            .NotEmpty()
            .When(x => !x.IsApproved)
            .WithMessage("Vui lòng nhập lý do khi từ chối yêu cầu hiệu chỉnh.");
    }
}

public class ReviewAdjustmentRequestLogCommandHandler : IRequestHandler<ReviewAdjustmentRequestLogCommand, DesignLogDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public ReviewAdjustmentRequestLogCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignLogDTO> Handle(ReviewAdjustmentRequestLogCommand request, CancellationToken cancellationToken)
    {
        var log = await _context.DesignLogs
            .Include(x => x.DesignWork)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (log == null)
        {
            throw new DataNotFoundException(nameof(DesignLog), request.Id);
        }

        if (log.LogType != DesignLogType.AdjustmentRequest)
        {
            throw new BusinessException(
                "Chỉ có thể duyệt log loại yêu cầu hiệu chỉnh.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        if (log.AdjustmentRequestStatus != AdjustmentRequestStatuses.PendingReview)
        {
            throw new BusinessException(
                "Yêu cầu hiệu chỉnh này đã được xử lý trước đó.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        if (request.IsApproved)
        {
            await ApproveAdjustmentRequestAsync(log, request.DecisionNote, cancellationToken);
        }
        else
        {
            RejectAdjustmentRequest(log, request.DecisionNote);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException(
                "Lượt hiệu chỉnh vừa được xử lý bởi một thao tác khác. Vui lòng tải lại và kiểm tra số lượt còn lại.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        return await _context.DesignLogs
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.VersionHistories)
            .Where(x => x.Id == log.Id)
            .ProjectTo<DesignLogDTO>(_mapper.ConfigurationProvider)
            .SingleAsync(cancellationToken);
    }

    private async Task ApproveAdjustmentRequestAsync(DesignLog log, string? decisionNote, CancellationToken cancellationToken)
    {
        var designWork = log.DesignWork;

        if (designWork.IsLocked)
        {
            throw new BusinessException(
                "Công việc thiết kế đã khóa. Không thể duyệt hiệu chỉnh trong công việc hiện tại.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        var allowedStatuses = new[] { DesignWorkStatus.Reviewing, DesignWorkStatus.Completed };
        if (!allowedStatuses.Contains(designWork.Status))
        {
            throw new BusinessException(
                "Chỉ có thể duyệt hiệu chỉnh khi thiết kế đang chờ duyệt hoặc đã có bản hoàn tất để phản hồi.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        var consumedSelectionId = await TryConsumePaidAdjustmentRoundAsync(designWork.Id, cancellationToken);
        if (!consumedSelectionId.HasValue)
        {
            throw new BusinessException(
                "Không còn lượt hiệu chỉnh đã thanh toán trong phạm vi brief ban đầu. Vui lòng yêu cầu khách mua thêm lượt hiệu chỉnh trước khi duyệt.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        log.AdjustmentRequestStatus = AdjustmentRequestStatuses.Approved;
        log.AdjustmentConsumedServiceSelectionId = consumedSelectionId;
        ApplyReviewMetadata(
            log,
            string.IsNullOrWhiteSpace(decisionNote)
                ? "Yêu cầu nằm trong phạm vi brief ban đầu và đã tiêu một lượt hiệu chỉnh."
                : decisionNote.Trim());

        designWork.Status = DesignWorkStatus.InProgress;
        designWork.LastModified = CoreHelper.SystemTimeNow;
        designWork.LastModifiedBy = _user.Username ?? "SYSTEM";
    }

    private void RejectAdjustmentRequest(DesignLog log, string? decisionNote)
    {
        log.AdjustmentRequestStatus = AdjustmentRequestStatuses.Rejected;
        ApplyReviewMetadata(
            log,
            string.IsNullOrWhiteSpace(decisionNote)
                ? "Yêu cầu vượt phạm vi brief ban đầu. Vui lòng tạo nhánh/yêu cầu mới hoặc đơn dịch vụ mới."
                : decisionNote.Trim());
    }

    private void ApplyReviewMetadata(DesignLog log, string decisionNote)
    {
        log.AdjustmentReviewedByAccountId = _user.Id.ToGuid();
        log.AdjustmentReviewedAt = CoreHelper.SystemTimeNow;
        log.AdjustmentDecisionNote = decisionNote;
        log.LastModified = CoreHelper.SystemTimeNow;
        log.LastModifiedBy = _user.Username ?? "SYSTEM";
    }

    private async Task<Guid?> TryConsumePaidAdjustmentRoundAsync(Guid designWorkId, CancellationToken cancellationToken)
    {
        var candidateSelections = await _context.ServiceSelections
            .Where(selection => selection.DesignWorkId == designWorkId
                && selection.IsLocked
                && selection.UsedAdjustmentRoundCount < selection.AdjustmentRoundLimit
                && _context.OrderItems.Any(item =>
                    item.ServiceSelectionId == selection.Id
                    && item.Order != null
                    && item.Order.OrderStatus != OrderStatuses.Pending
                    && item.Order.OrderStatus != OrderStatuses.Cancelled
                    && item.Order.Invoice != null
                    && item.Order.Invoice.PaymentStatus == InvoiceStatuses.Paid))
            .OrderBy(x => x.Created)
            .ToListAsync(cancellationToken);

        foreach (var selection in candidateSelections)
        {
            selection.UsedAdjustmentRoundCount += 1;
            selection.LastModified = CoreHelper.SystemTimeNow;
            selection.LastModifiedBy = _user.Username ?? "SYSTEM";
            return selection.Id;
        }

        return null;
    }
}
