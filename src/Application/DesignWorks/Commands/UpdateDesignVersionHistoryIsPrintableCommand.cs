using System;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

/// <summary>
/// DEPRECATED — Use ReviewFileVersionCommand instead.
/// This command is kept for backward compatibility only.
/// It now routes through FileReviewStatus (sets ACCEPTED or REJECTED)
/// to keep IsPrintable and FileReviewStatus in sync.
/// Will be removed in a future release.
/// </summary>
[Authorize(Roles = Roles.StaffOrManager)]
public record UpdateDesignVersionHistoryIsPrintableCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid Id { get; init; }

    /// <summary>
    /// true = mark file as ACCEPTED (printable); false = mark as REJECTED.
    /// If false, a generic rejection note will be applied.
    /// For a custom note, use ReviewFileVersionCommand.
    /// </summary>
    public bool IsPrintable { get; init; } = true;
}

public class UpdateDesignVersionHistoryIsPrintableCommandHandler
    : IRequestHandler<UpdateDesignVersionHistoryIsPrintableCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateDesignVersionHistoryIsPrintableCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<bool> Handle(
        UpdateDesignVersionHistoryIsPrintableCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.DesignVersionHistorys
            .FirstOrDefaultAsync(dw => dw.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            // Fixed: was using nameof(DesignWork) by mistake — entity is DesignVersionHistory
            throw new DataNotFoundException(nameof(DesignVersionHistory), request.Id);
        }

        var now = Domain.Utils.CoreHelper.SystemTimeNow;

        // Sync both IsPrintable (legacy) and FileReviewStatus (new) together
        entity.IsPrintable = request.IsPrintable;
        entity.FileReviewStatus = request.IsPrintable
            ? FileReviewStatuses.Accepted
            : FileReviewStatuses.Rejected;

        if (!request.IsPrintable && string.IsNullOrEmpty(entity.FileReviewNote))
        {
            entity.FileReviewNote = "File không đạt tiêu chuẩn in. Vui lòng sử dụng ReviewFileVersionCommand để ghi rõ lý do.";
        }

        entity.LastModified = now;
        entity.LastModifiedBy = _user.Username ?? "SYSTEM";

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
