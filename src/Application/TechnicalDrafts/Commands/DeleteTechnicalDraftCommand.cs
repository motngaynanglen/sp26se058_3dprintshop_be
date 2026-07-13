using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.TechnicalDrafts.Commands;

[Authorize(Roles = Roles.StaffOrManager)]
public record DeleteTechnicalDraftCommand(Guid Id) : IRequest<bool>;

public class DeleteTechnicalDraftCommandHandler : IRequestHandler<DeleteTechnicalDraftCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteTechnicalDraftCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<bool> Handle(DeleteTechnicalDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await _context.TechnicalDrafts
            .Include(x => x.DesignVersionHistory)
                .ThenInclude(x => x.DesignWork)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (draft == null)
        {
            throw new DataNotFoundException(nameof(TechnicalDraft), request.Id);
        }

        if (draft.IsConfirmed || draft.DesignVersionHistory.DesignWork.IsLocked)
        {
            throw new BusinessException("Bản nháp kỹ thuật đã được xác nhận hoặc công việc thiết kế đã khóa, không thể xóa.");
        }

        draft.Deleted = CoreHelper.SystemTimeNow;
        draft.DeletedBy = _user.Username;
        draft.LastModified = CoreHelper.SystemTimeNow;
        draft.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
