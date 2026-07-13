using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

[Authorize(Roles = Roles.CUSTOMER)]
public record UpdateDesignWorkIsApproveCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid Id { get; init; }
}

public class UpdateDesignWorkIsApproveCommandHandler : IRequestHandler<UpdateDesignWorkIsApproveCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateDesignWorkIsApproveCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }
    public async Task<bool> Handle(UpdateDesignWorkIsApproveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.DesignVersionHistorys
            .Include(x => x.DesignWork)
            .FirstOrDefaultAsync(dw => dw.Id == request.Id, cancellationToken);
        if (entity == null)
        {
            throw new DataNotFoundException(nameof(DesignVersionHistory), request.Id);
        }

        if (entity.DesignWork.IsLocked)
        {
            throw new BusinessException("Công việc thiết kế đã bị khóa.");
        }

        var userId = _user.Id.ToGuid();
        var customerOwnsDesignWork = await _context.Customers
            .AsNoTracking()
            .AnyAsync(x => x.AccountId == userId && x.Id == entity.DesignWork.CustomerId, cancellationToken);

        if (!customerOwnsDesignWork)
        {
            throw new ForbiddenAccessException();
        }

        // Cập nhật trạng thái phê duyệt
        entity.IsApproved = true; // Hoặc false tùy theo yêu cầu
        entity.DesignWork.Status = DesignWorkStatus.Completed;
        entity.DesignWork.IsLocked = true;
        entity.DesignWork.LastModified = CoreHelper.SystemTimeNow;
        entity.DesignWork.LastModifiedBy = _user.Username;

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = entity.DesignWorkId,
            AccountId = _user.Id != null ? Guid.Parse(_user.Id) : null,
            LogType = DesignLogType.StatusChange,
            Content = "Khách hàng đã duyệt phiên bản cuối. Công việc thiết kế được chuyển sang trạng thái đã nghiệm thu và khóa lịch sử.",
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "SYSTEM"
        });

        await _context.SaveChangesAsync(cancellationToken);
        return true; // Trả về true nếu cập nhật thành công
    }
}
