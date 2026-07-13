using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

/// <summary>
/// Duyệt một DesignVersionHistory file. Người duyệt phụ thuộc loại công việc:
///  - PRINT_SERVICE (khách upload file để in): STAFF/MANAGER duyệt chất lượng in.
///  - DESIGN_SERVICE / legacy (nhân viên làm thiết kế): KHÁCH HÀNG (chủ sở hữu) duyệt; MANAGER giám sát.
/// Authorize mở cho cả 3 role; phân quyền chi tiết theo WorkType nằm trong handler.
/// </summary>
[Authorize(Roles = Roles.CustomerStaffManager)]
public record ReviewFileVersionCommand : IRequest<DesignLogDTO>
{
    /// <summary>ID of the DesignVersionHistory to review.</summary>
    [JsonIgnore]
    public Guid VersionHistoryId { get; init; }

    /// <summary>"ACCEPTED" or "REJECTED"</summary>
    [DefaultValue("ACCEPTED")]
    public string ReviewStatus { get; init; } = string.Empty;

    /// <summary>
    /// Rejection reason or fix instructions shown to the customer.
    /// Required when ReviewStatus = REJECTED.
    /// </summary>
    [DefaultValue(null)]
    public string? ReviewNote { get; init; }
}

public class ReviewFileVersionCommandValidator : AbstractValidator<ReviewFileVersionCommand>
{
    public ReviewFileVersionCommandValidator()
    {
        RuleFor(x => x.VersionHistoryId)
            .NotEmpty().WithMessage("ID phiên bản file không hợp lệ.");

        RuleFor(x => x.ReviewStatus)
            .NotEmpty().WithMessage("Trạng thái duyệt không được để trống.")
            .Must(FileReviewStatuses.IsValid)
            .WithMessage($"Trạng thái duyệt phải là {FileReviewStatuses.Accepted} hoặc {FileReviewStatuses.Rejected}.");

        // REJECTED must have a note
        RuleFor(x => x.ReviewNote)
            .NotEmpty()
            .When(x => x.ReviewStatus == FileReviewStatuses.Rejected)
            .WithMessage("Vui lòng nhập lý do từ chối để thông báo cho khách hàng.");

        RuleFor(x => x.ReviewNote)
            .MaximumLength(2000)
            .WithMessage("Lý do không được quá 2000 ký tự.");
    }
}

public class ReviewFileVersionCommandHandler : IRequestHandler<ReviewFileVersionCommand, DesignLogDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    private readonly ILogger<ReviewFileVersionCommandHandler> _logger;

    public ReviewFileVersionCommandHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IUser user,
        ILogger<ReviewFileVersionCommandHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
        _logger = logger;
    }

    public async Task<DesignLogDTO> Handle(ReviewFileVersionCommand request, CancellationToken cancellationToken)
    {
        // Load version history + parent DesignWork in one query
        var version = await _context.DesignVersionHistorys
            .Include(v => v.DesignWork)
            .FirstOrDefaultAsync(v => v.Id == request.VersionHistoryId, cancellationToken);

        if (version == null)
        {
            throw new DataNotFoundException(nameof(DesignVersionHistory), request.VersionHistoryId);
        }

        var designWork = version.DesignWork;

        if (designWork.IsLocked)
        {
            throw new BusinessException(
                "Công việc thiết kế đã khóa. Không thể duyệt file trong công việc đã hoàn tất.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        // --- Phân quyền theo loại công việc ---
        // PRINT_SERVICE: khách upload file để in → STAFF/MANAGER duyệt.
        // DESIGN_SERVICE (hoặc legacy null): nhân viên làm thiết kế → KHÁCH HÀNG (chủ sở hữu) duyệt; MANAGER giám sát.
        bool isPrintService = designWork.WorkType == DesignWorkTypes.PrintService;
        bool isCustomer = _user.Role == Roles.CUSTOMER;

        if (isPrintService)
        {
            if (_user.Role != Roles.STAFF && _user.Role != Roles.MANAGER)
            {
                throw new ForbiddenAccessException("Chỉ nhân viên được duyệt file đối với đơn in theo yêu cầu.");
            }
        }
        else
        {
            if (isCustomer)
            {
                var reviewerId = _user.Id.ToGuid();
                var reviewerCustomer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.AccountId == reviewerId, cancellationToken);
                if (reviewerCustomer == null || designWork.CustomerId != reviewerCustomer.Id)
                {
                    throw new ForbiddenAccessException("Bạn không có quyền duyệt thiết kế này.");
                }
            }
            else if (_user.Role != Roles.MANAGER)
            {
                // STAFF không tự duyệt thiết kế trong dịch vụ thiết kế — việc này thuộc về khách hàng.
                throw new ForbiddenAccessException("Nhân viên không duyệt thiết kế trong dịch vụ thiết kế — việc này thuộc về khách hàng.");
            }
        }

        // Idempotency guard: already reviewed with the same status → skip silently
        if (version.FileReviewStatus == request.ReviewStatus)
        {
            _logger.LogInformation(
                "File version {VersionId} already has status {Status} — skipping review.",
                version.Id, request.ReviewStatus);

            // Return existing notification log if any, else create new one
            return await BuildResponseAsync(version.Id, cancellationToken);
        }

        var now = CoreHelper.SystemTimeNow;
        var reviewerName = _user.Username ?? (isCustomer ? "Khách hàng" : "Nhân viên");
        bool isAccepted = request.ReviewStatus == FileReviewStatuses.Accepted;

        // --- Update FileReviewStatus on version ---
        version.FileReviewStatus = request.ReviewStatus;
        version.FileReviewNote = request.ReviewNote;
        version.IsPrintable = isAccepted;   // Sync legacy boolean for backward compat
        version.LastModified = now;
        version.LastModifiedBy = reviewerName;

        // --- Create COMMUNICATION log visible to both sides --- (nội dung theo người duyệt)
        var fileName = ExtractFileName(version.FileUrl);
        string notificationContent = isAccepted
            ? (isCustomer
                ? $"Khách hàng đã duyệt thiết kế \"{fileName}\"."
                : $"Nhân viên xác nhận file \"{fileName}\" đủ tiêu chuẩn in 3D.")
            : (isCustomer
                ? $"Khách hàng từ chối thiết kế \"{fileName}\". Lý do: {request.ReviewNote}. Nhân viên vui lòng chỉnh sửa và gửi lại."
                : $"Nhân viên từ chối file \"{fileName}\". Lý do: {request.ReviewNote}. Vui lòng upload lại file đã chỉnh sửa.");

        var notificationLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = designWork.Id,
            AccountId = _user.Id.ToGuid() == Guid.Empty ? null : _user.Id.ToGuid(),
            Content = notificationContent,
            LogType = DesignLogType.Communication,
            IsAI = false,
            Created = now,
            CreatedBy = reviewerName,
            LastModified = now,
            LastModifiedBy = reviewerName
        };

        _context.DesignLogs.Add(notificationLog);

        // --- POD only: transition DesignWork to SKETCHING if ALL files are REJECTED ---
        if (designWork.WorkType == DesignWorkTypes.PrintService && !isAccepted)
        {
            await CheckAndApplyAllRejectedTransitionAsync(designWork, now, reviewerName, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "File version {VersionId} reviewed as {Status} by {Staff} for DesignWork {WorkId}.",
            version.Id, request.ReviewStatus, reviewerName, designWork.Id);

        return await BuildResponseAsync(notificationLog.Id, cancellationToken);
    }

    /// <summary>
    /// For POD: if ALL DesignVersionHistorys of this DesignWork are REJECTED after this review,
    /// transition DesignWork back to SKETCHING so the customer can re-upload.
    /// </summary>
    private async Task CheckAndApplyAllRejectedTransitionAsync(
        DesignWork designWork,
        DateTimeOffset now,
        string staffName,
        CancellationToken ct)
    {
        // Load all version histories for this work (excluding the one just reviewed — already updated in-memory)
        var allVersionStatuses = await _context.DesignVersionHistorys
            .Where(v => v.DesignWorkId == designWork.Id && !v.Deleted.HasValue)
            .Select(v => v.FileReviewStatus)
            .ToListAsync(ct);

        bool allRejected = allVersionStatuses.All(s => s == FileReviewStatuses.Rejected);

        if (!allRejected) return;

        // Allow REVIEWING → SKETCHING only (guard against other states)
        if (designWork.Status != DesignWorkStatus.Reviewing) return;

        designWork.Status = DesignWorkStatus.Sketching;
        designWork.LastModified = now;
        designWork.LastModifiedBy = staffName;

        var statusLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = designWork.Id,
            Content = "Tất cả file đã bị từ chối. Yêu cầu in chuyển về trạng thái chờ khách upload lại file.",
            LogType = DesignLogType.StatusChange,
            IsAI = false,
            Created = now.AddMilliseconds(1),
            CreatedBy = "SYSTEM",
            LastModified = now.AddMilliseconds(1),
            LastModifiedBy = "SYSTEM"
        };

        _context.DesignLogs.Add(statusLog);

        _logger.LogInformation(
            "All files rejected for POD DesignWork {WorkId} — transitioning to SKETCHING.",
            designWork.Id);
    }

    private async Task<DesignLogDTO> BuildResponseAsync(Guid logId, CancellationToken ct)
    {
        return await _context.DesignLogs
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.VersionHistories)
            .Where(x => x.Id == logId)
            .ProjectTo<DesignLogDTO>(_mapper.ConfigurationProvider)
            .SingleAsync(ct);
    }

    private static string ExtractFileName(string fileUrl)
    {
        try
        {
            var path = fileUrl.Split('?')[0]; // strip query string
            return Path.GetFileName(path);
        }
        catch
        {
            return fileUrl;
        }
    }
}
