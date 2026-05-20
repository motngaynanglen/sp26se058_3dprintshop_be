using System.ComponentModel;
using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignLogs.Commands;

[Authorize(Roles = Roles.CustomerStaffManager)]
public record CreateAdjustmentRequestLogCommand : IRequest<DesignLogDTO>
{
    public Guid DesignWorkId { get; init; }
    public Guid? ParentLogId { get; init; }

    [DefaultValue("Tổng hợp yêu cầu hiệu chỉnh theo brief ban đầu từ nội dung trao đổi với khách.")]
    public string? Content { get; init; }

    public List<string>? ImageUrls { get; init; }
}

public class CreateAdjustmentRequestLogCommandValidator : AbstractValidator<CreateAdjustmentRequestLogCommand>
{
    public CreateAdjustmentRequestLogCommandValidator()
    {
        RuleFor(x => x.DesignWorkId).NotEmpty();

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content) || (x.ImageUrls?.Any() ?? false))
            .WithMessage("Vui lòng nhập nội dung hoặc đính kèm hình ảnh cho yêu cầu hiệu chỉnh.");
    }
}

public class CreateAdjustmentRequestLogCommandHandler : IRequestHandler<CreateAdjustmentRequestLogCommand, DesignLogDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CreateAdjustmentRequestLogCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignLogDTO> Handle(CreateAdjustmentRequestLogCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid();
        var isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;

        var designWork = await _context.DesignWorks
            .FirstOrDefaultAsync(x => x.Id == request.DesignWorkId, cancellationToken);

        if (designWork == null)
        {
            throw new DataNotFoundException(nameof(DesignWork), request.DesignWorkId);
        }

        if (!isStaffOrManager)
        {
            var customerOwnsDesignWork = await _context.Customers
                .AsNoTracking()
                .AnyAsync(x => x.AccountId == userId && x.Id == designWork.CustomerId, cancellationToken);

            if (!customerOwnsDesignWork)
            {
                throw new ForbiddenAccessException("Yêu cầu tài khoản khách hàng sở hữu công việc thiết kế này.");
            }
        }

        if (designWork.IsLocked)
        {
            throw new BusinessException(
                "Công việc thiết kế đã khóa. Vui lòng tạo nhánh/yêu cầu mới hoặc tạo đơn dịch vụ mới để tiếp tục chỉnh sửa.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        var allowedStatuses = new[] { DesignWorkStatus.InProgress, DesignWorkStatus.Reviewing, DesignWorkStatus.Completed };
        if (!allowedStatuses.Contains(designWork.Status))
        {
            throw new BusinessException(
                "Chỉ có thể tạo yêu cầu hiệu chỉnh khi thiết kế đang được xử lý, chờ duyệt hoặc đã có bản hoàn tất để phản hồi.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        if (request.ParentLogId.HasValue)
        {
            var parentLogExists = await _context.DesignLogs
                .AnyAsync(x => x.Id == request.ParentLogId && x.DesignWorkId == request.DesignWorkId, cancellationToken);

            if (!parentLogExists)
            {
                throw new DataNotFoundException(nameof(DesignLog), request.ParentLogId.Value);
            }
        }

        var log = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = request.DesignWorkId,
            ParentLogId = request.ParentLogId,
            AccountId = userId == Guid.Empty ? null : userId,
            Content = request.Content?.Trim(),
            LogType = DesignLogType.AdjustmentRequest,
            AdjustmentRequestStatus = AdjustmentRequestStatuses.PendingReview,
            IsAI = false,
            Metadata = request.ImageUrls != null ? JsonSerializer.Serialize(request.ImageUrls) : null,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "SYSTEM",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "SYSTEM"
        };

        _context.DesignLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.DesignLogs
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.VersionHistories)
            .Where(x => x.Id == log.Id)
            .ProjectTo<DesignLogDTO>(_mapper.ConfigurationProvider)
            .SingleAsync(cancellationToken);
    }
}
