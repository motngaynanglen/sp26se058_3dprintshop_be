using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignLogs.Commands;

public record UpdateDesignLogCommand : IRequest<DesignLogDTO>
{
    public Guid Id { get; init; }
    public bool IsRead { get; init; }
}

public class UpdateDesignLogCommandHandler : IRequestHandler<UpdateDesignLogCommand, DesignLogDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public UpdateDesignLogCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignLogDTO> Handle(UpdateDesignLogCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.DesignLogs
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (entity == null) throw new NotFoundException(nameof(DesignLog), "Không tìm thấy log thiết kế với id " + request.Id);

        //entity.IsRead = request.IsRead;
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DesignLogDTO>(entity);
    }
}
