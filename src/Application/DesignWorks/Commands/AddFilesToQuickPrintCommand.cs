using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

/// <summary>
/// Unified Quick Print file submission command — handles two paths via DesignWorkId:
/// <list type="bullet">
///   <item><description>
///     <b>DesignWorkId = null</b>: Create a new Quick Print request (customer's first upload).
///     Requires <c>ProjectName</c>.
///   </description></item>
///   <item><description>
///     <b>DesignWorkId = &lt;id&gt;</b>: Re-upload files to an existing SKETCHING request
///     (all files were previously rejected by staff).
///   </description></item>
/// </list>
/// Replaces the old <c>CheckoutQuickPrintCommand</c> which only handled the create path.
/// </summary>
[Authorize(Roles = Roles.CUSTOMER)]
public record AddFilesToQuickPrintCommand : IRequest<DesignWorkDTO>
{
    /// <summary>
    /// Injected from route by the endpoint.
    /// Null when creating a new Quick Print request (POST /quick-print).
    /// Set when re-uploading to an existing SKETCHING request (POST /{id}/add-files).
    /// </summary>
    [JsonIgnore]
    public Guid? DesignWorkId { get; init; }

    /// <summary>Required when creating a new request (DesignWorkId is null).</summary>
    [DefaultValue("Yêu cầu in 3D")]
    public string? ProjectName { get; init; }

    /// <summary>Optional description / printing instructions (color, resolution, etc.).</summary>
    [DefaultValue(null)]
    public string? Description { get; init; }

    /// <summary>1–5 pre-uploaded file URLs (.glb / .stl / .obj).</summary>
    public List<string> FileUrls { get; init; } = new();

    /// <summary>Optional note shown on the timeline (used on re-upload path).</summary>
    [DefaultValue(null)]
    public string? Note { get; init; }
}

public class AddFilesToQuickPrintCommandValidator : AbstractValidator<AddFilesToQuickPrintCommand>
{
    public AddFilesToQuickPrintCommandValidator()
    {
        // ProjectName is required only when creating a brand-new request
        When(x => !x.DesignWorkId.HasValue, () =>
        {
            RuleFor(x => x.ProjectName)
                .NotEmpty().WithMessage("Tên dự án không được để trống.")
                .MaximumLength(200).WithMessage("Tên dự án không được quá 200 ký tự.");
        });

        RuleFor(x => x.FileUrls)
            .NotEmpty().WithMessage("Bạn phải cung cấp ít nhất một file 3D.")
            .Must(list => list.Count <= 5).WithMessage("Chỉ được gửi tối đa 5 file cho một lần upload.");

        RuleForEach(x => x.FileUrls)
            .NotEmpty().WithMessage("Đường dẫn file không được để trống.")
            .Must(url => url.Contains(".glb") || url.Contains(".stl") || url.Contains(".obj"))
            .WithMessage("Hệ thống chỉ hỗ trợ định dạng .glb, .stl hoặc .obj.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được quá 1000 ký tự.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không được quá 1000 ký tự.");
    }
}

public class AddFilesToQuickPrintCommandHandler : IRequestHandler<AddFilesToQuickPrintCommand, DesignWorkDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    private readonly ILogger<AddFilesToQuickPrintCommandHandler> _logger;

    public AddFilesToQuickPrintCommandHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IUser user,
        ILogger<AddFilesToQuickPrintCommandHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
        _logger = logger;
    }

    public async Task<DesignWorkDTO> Handle(AddFilesToQuickPrintCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid();

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AccountId == userId, cancellationToken);

        if (customer == null) throw new ForbiddenAccessException();

        // Route to the correct path based on whether DesignWorkId is provided
        return request.DesignWorkId.HasValue
            ? await HandleReupload(request, request.DesignWorkId.Value, customer.Id, userId, cancellationToken)
            : await HandleCreateNew(request, customer.Id, userId, cancellationToken);
    }

    // ─── Path A: Create a brand-new Quick Print request ──────────────────────

    private async Task<DesignWorkDTO> HandleCreateNew(
        AddFilesToQuickPrintCommand request,
        Guid customerId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var now = CoreHelper.SystemTimeNow;
        var actor = _user.Username ?? "CUSTOMER";
        var newWorkId = Guid.NewGuid();

        var designWork = new DesignWork
        {
            Id = newWorkId,
            RootDesignWorkId = newWorkId,
            RelationshipType = DesignRelationshipType.Original,
            Name = request.ProjectName ?? "Yêu cầu in 3D nhanh",
            CustomerId = customerId,
            WorkType = DesignWorkTypes.PrintService,
            // POD starts at REVIEWING — staff immediately sees files to review
            Status = DesignWorkStatus.Reviewing,
            Created = now,
            CreatedBy = actor,
            LastModified = now,
            LastModifiedBy = actor
        };

        var fileLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = newWorkId,
            AccountId = userId == Guid.Empty ? null : userId,
            Content = request.Description ?? "Khách hàng gửi file in gốc.",
            Metadata = JsonSerializer.Serialize(request.FileUrls),
            LogType = DesignLogType.VersionUpdate,
            Created = now,
            CreatedBy = actor,
            LastModified = now,
            LastModifiedBy = actor
        };

        int versionCounter = 1;
        foreach (var url in request.FileUrls)
        {
            _context.DesignVersionHistorys.Add(new DesignVersionHistory
            {
                Id = Guid.NewGuid(),
                DesignWorkId = newWorkId,
                DesignLogId = fileLog.Id,
                UploaderId = userId,
                FileUrl = url,
                Tilte = $"File gốc: {Path.GetFileName(url.Split('?')[0])}",
                VersionNumber = versionCounter++,
                IsPreviewable = true,
                IsApproved = false,
                FileReviewStatus = FileReviewStatuses.Pending,
                IsPrintable = false, // set to true by ReviewFileVersionCommand when ACCEPTED
                Created = now,
                CreatedBy = actor,
                LastModified = now,
                LastModifiedBy = actor
            });
        }

        var systemLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = newWorkId,
            Content = "Hệ thống: Khởi tạo luồng PRINT_SERVICE. Chờ nhân viên kiểm tra file và báo giá.",
            LogType = DesignLogType.System,
            Created = now.AddMilliseconds(10),
            CreatedBy = "SYSTEM",
            LastModified = now.AddMilliseconds(10),
            LastModifiedBy = "SYSTEM"
        };

        _context.DesignWorks.Add(designWork);
        _context.DesignLogs.AddRange(fileLog, systemLog);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Customer {CustomerId} created new QuickPrint DesignWork {WorkId} with {Count} file(s).",
            customerId, newWorkId, request.FileUrls.Count);

        return _mapper.Map<DesignWorkDTO>(designWork);
    }

    // ─── Path B: Re-upload files to a SKETCHING request ──────────────────────

    private async Task<DesignWorkDTO> HandleReupload(
        AddFilesToQuickPrintCommand request,
        Guid designWorkId,
        Guid customerId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var designWork = await _context.DesignWorks
            .FirstOrDefaultAsync(dw => dw.Id == designWorkId, cancellationToken);

        if (designWork == null)
            throw new DataNotFoundException(nameof(DesignWork), designWorkId);

        // Ownership: customer must own the work
        if (designWork.CustomerId != customerId)
            throw new ForbiddenAccessException("Bạn không có quyền thêm file cho yêu cầu in này.");

        // Only for Quick Print
        if (designWork.WorkType != DesignWorkTypes.PrintService)
            throw new BusinessException(
                "Chỉ có thể thêm file vào yêu cầu in nhanh (PRINT_SERVICE).",
                ResponseCodeConstants.VAL_INVALID_STATE);

        // Must be in SKETCHING — all previous files were rejected, customer must re-upload
        if (designWork.Status != DesignWorkStatus.Sketching)
            throw new BusinessException(
                $"Chỉ có thể upload lại file khi yêu cầu in đang ở trạng thái SKETCHING. " +
                $"Trạng thái hiện tại: {designWork.Status}",
                ResponseCodeConstants.VAL_INVALID_STATE);

        if (designWork.IsLocked)
            throw new BusinessException(
                "Yêu cầu in đã hoàn tất và bị khóa.",
                ResponseCodeConstants.VAL_INVALID_STATE);

        var now = CoreHelper.SystemTimeNow;
        var actor = _user.Username ?? "CUSTOMER";

        // Continue VersionNumber after the existing max (don't restart from 1)
        var maxVersion = await _context.DesignVersionHistorys
            .Where(v => v.DesignWorkId == designWorkId)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var fileLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = designWorkId,
            AccountId = userId == Guid.Empty ? null : userId,
            Content = request.Note ?? "Khách hàng upload lại file sau khi được nhân viên hướng dẫn chỉnh sửa.",
            Metadata = JsonSerializer.Serialize(request.FileUrls),
            LogType = DesignLogType.VersionUpdate,
            Created = now,
            CreatedBy = actor,
            LastModified = now,
            LastModifiedBy = actor
        };

        _context.DesignLogs.Add(fileLog);

        int versionCounter = maxVersion + 1;
        foreach (var url in request.FileUrls)
        {
            _context.DesignVersionHistorys.Add(new DesignVersionHistory
            {
                Id = Guid.NewGuid(),
                DesignWorkId = designWorkId,
                DesignLogId = fileLog.Id,
                UploaderId = userId,
                FileUrl = url,
                Tilte = $"File upload lại v{versionCounter}: {Path.GetFileName(url.Split('?')[0])}",
                VersionNumber = versionCounter++,
                IsPreviewable = true,
                IsApproved = false,
                FileReviewStatus = FileReviewStatuses.Pending,
                IsPrintable = false,
                Created = now,
                CreatedBy = actor,
                LastModified = now,
                LastModifiedBy = actor
            });
        }

        // Transition back to REVIEWING — staff must review the new files
        designWork.Status = DesignWorkStatus.Reviewing;
        designWork.LastModified = now;
        designWork.LastModifiedBy = actor;

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = designWorkId,
            Content = "Khách hàng đã upload lại file. Yêu cầu in chờ nhân viên kiểm tra.",
            LogType = DesignLogType.StatusChange,
            Created = now.AddMilliseconds(5),
            CreatedBy = "SYSTEM",
            LastModified = now.AddMilliseconds(5),
            LastModifiedBy = "SYSTEM"
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Customer {CustomerId} re-uploaded {Count} file(s) to QuickPrint DesignWork {WorkId}.",
            customerId, request.FileUrls.Count, designWorkId);

        return await _context.DesignWorks
            .AsNoTracking()
            .Where(dw => dw.Id == designWorkId)
            .ProjectTo<DesignWorkDTO>(_mapper.ConfigurationProvider)
            .SingleAsync(cancellationToken);
    }
}
