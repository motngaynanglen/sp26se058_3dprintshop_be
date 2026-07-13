using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignVersions.Commands;

[Authorize(Roles = Roles.StaffOrManager)]
public record UpdateDesignVersionPrintableCommand : IRequest<bool>
{
    public Guid Id { get; init; }
    public bool IsPrintable { get; init; }
}

public class UpdateDesignVersionPrintableCommandHandler : IRequestHandler<UpdateDesignVersionPrintableCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateDesignVersionPrintableCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<bool> Handle(UpdateDesignVersionPrintableCommand request, CancellationToken cancellationToken)
    {
        var version = await _context.DesignVersionHistorys
            .Include(x => x.DesignWork)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (version == null)
        {
            throw new DataNotFoundException(nameof(DesignVersionHistory), request.Id);
        }

        if (version.DesignWork.IsLocked)
        {
            throw new BusinessException("Công việc thiết kế đã bị khóa, không thể cập nhật trạng thái file.");
        }

        version.IsPrintable = request.IsPrintable;
        version.LastModified = CoreHelper.SystemTimeNow;
        version.LastModifiedBy = _user.Username;

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = version.DesignWorkId,
            AccountId = _user.Id != null ? Guid.Parse(_user.Id) : null,
            LogType = DesignLogType.StatusChange,
            Content = request.IsPrintable
                ? $"File phiên bản {version.VersionNumber} đã được đánh dấu có thể in."
                : $"File phiên bản {version.VersionNumber} đã được bỏ đánh dấu có thể in.",
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "SYSTEM"
        });

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
