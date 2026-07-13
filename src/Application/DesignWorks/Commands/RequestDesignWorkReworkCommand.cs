using System.ComponentModel;
using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

[Authorize(Roles = Roles.CUSTOMER)]
public record RequestDesignWorkReworkCommand : IRequest<DesignWorkDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }

    [DefaultValue("Chỉnh lại chi tiết khuôn mặt và tăng độ dày thành in")]
    public string? Note { get; init; }

    [DefaultValue(null)]
    public Guid? SourceVersionHistoryId { get; init; }
}

public class RequestDesignWorkReworkCommandValidator : AbstractValidator<RequestDesignWorkReworkCommand>
{
    public RequestDesignWorkReworkCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class RequestDesignWorkReworkCommandHandler : IRequestHandler<RequestDesignWorkReworkCommand, DesignWorkDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public RequestDesignWorkReworkCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignWorkDTO> Handle(RequestDesignWorkReworkCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid();
        var customer = await _context.Customers
            .FirstOrDefaultAsync(x => x.AccountId == userId, cancellationToken);

        if (customer == null)
        {
            throw new ForbiddenAccessException("Yêu cầu tài khoản khách hàng.");
        }

        var parentWork = await _context.DesignWorks
            .Include(x => x.VersionHistories)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CustomerId == customer.Id, cancellationToken);

        if (parentWork == null)
        {
            throw new DataNotFoundException(nameof(DesignWork), request.Id);
        }

        var sourceVersion = request.SourceVersionHistoryId.HasValue
            ? parentWork.VersionHistories.FirstOrDefault(x => x.Id == request.SourceVersionHistoryId.Value)
            : parentWork.VersionHistories
                .OrderByDescending(x => x.IsApproved)
                .ThenByDescending(x => x.VersionNumber)
                .FirstOrDefault();

        var newWorkId = Guid.NewGuid();
        var revisionWork = new DesignWork
        {
            Id = newWorkId,
            ParentDesignWorkId = parentWork.Id,
            RootDesignWorkId = parentWork.RootDesignWorkId == Guid.Empty ? parentWork.Id : parentWork.RootDesignWorkId,
            RelationshipType = DesignRelationshipType.Revision,
            Name = $"Chỉnh sửa: {parentWork.Name}",
            CustomerId = customer.Id,
            MainAssignedStaffId = parentWork.MainAssignedStaffId,
            BaseImageUrl = sourceVersion?.FileUrl ?? parentWork.BaseImageUrl,
            Status = DesignWorkStatus.Sketching,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "CUSTOMER",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "CUSTOMER"
        };

        var requestLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = revisionWork.Id,
            AccountId = userId,
            Content = request.Note ?? "Khách hàng yêu cầu chỉnh sửa thêm từ công việc thiết kế đã hoàn tất.",
            LogType = DesignLogType.Communication,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "CUSTOMER"
        };

        var systemLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = revisionWork.Id,
            Content = $"Tạo phiên bản chỉnh sửa từ công việc thiết kế {parentWork.Id}.",
            LogType = DesignLogType.System,
            Created = CoreHelper.SystemTimeNow.AddMilliseconds(10),
            CreatedBy = "SYSTEM"
        };

        _context.DesignWorks.Add(revisionWork);
        _context.DesignLogs.AddRange(requestLog, systemLog);

        if (sourceVersion != null)
        {
            _context.DesignVersionHistorys.Add(new DesignVersionHistory
            {
                Id = Guid.NewGuid(),
                DesignWorkId = revisionWork.Id,
                DesignLogId = systemLog.Id,
                UploaderId = sourceVersion.UploaderId,
                Tilte = $"Base revision from v{sourceVersion.VersionNumber}",
                FileUrl = sourceVersion.FileUrl,
                VersionNumber = 1,
                IsPreviewable = sourceVersion.IsPreviewable,
                IsApproved = false,
                IsPrintable = sourceVersion.IsPrintable,
                Created = CoreHelper.SystemTimeNow.AddMilliseconds(20),
                CreatedBy = "SYSTEM"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DesignWorkDTO>(revisionWork);
    }
}
