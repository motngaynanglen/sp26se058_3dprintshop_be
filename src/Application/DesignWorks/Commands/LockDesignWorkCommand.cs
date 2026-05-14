using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

[Authorize(Roles = Roles.CUSTOMER + "," + Roles.STAFF + "," + Roles.MANAGER)]
public record LockDesignWorkCommand : IRequest<DesignWorkDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }
}

public class LockDesignWorkCommandHandler : IRequestHandler<LockDesignWorkCommand, DesignWorkDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public LockDesignWorkCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignWorkDTO> Handle(LockDesignWorkCommand request, CancellationToken cancellationToken)
    {
        var designWork = await _context.DesignWorks
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (designWork == null)
        {
            throw new DataNotFoundException(nameof(DesignWork), request.Id);
        }

        if (!designWork.IsLocked)
        {
            designWork.IsLocked = true;
            designWork.LastModified = CoreHelper.SystemTimeNow;
            designWork.LastModifiedBy = _user.Username;

            _context.DesignLogs.Add(new DesignLog
            {
                Id = Guid.NewGuid(),
                DesignWorkId = designWork.Id,
                AccountId = _user.Id != null ? Guid.Parse(_user.Id) : null,
                LogType = DesignLogType.StatusChange,
                Content = "DesignWork đã được khóa để bảo toàn lịch sử và file đã chốt.",
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "SYSTEM"
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        return _mapper.Map<DesignWorkDTO>(designWork);
    }
}
