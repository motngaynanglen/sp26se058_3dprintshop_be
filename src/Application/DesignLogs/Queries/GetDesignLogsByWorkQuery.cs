using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.DesignLogs.Queries;

[Authorize(Roles = Roles.CustomerStaffManager)]
public record GetDesignLogsByWorkQuery(Guid DesignWorkId) : IRequest<List<DesignLogDTO>>
{
    [JsonIgnore]
    public Guid Id { get; init; }
}

public class GetDesignLogsByWorkQueryHandler : IRequestHandler<GetDesignLogsByWorkQuery, List<DesignLogDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetDesignLogsByWorkQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<List<DesignLogDTO>> Handle(GetDesignLogsByWorkQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DesignLogs
            .AsNoTracking()
            .Include(l => l.DesignWork)
            .Include(l => l.VersionHistories)
            .Where(l => l.DesignWorkId == request.DesignWorkId);

        bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;

        if (!isStaffOrManager)
        {
            var userId = _user.Id.ToGuid();
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AccountId == userId, cancellationToken);

            if (customer == null)
            {
                throw new ForbiddenAccessException();
            }

            query = query.Where(l => l.DesignWork.CustomerId == customer.Id);
            query = query.Where(l => l.LogType != "INTERNAL_NOTE");
        }
        var logs = await query
            .OrderBy(l => l.Created)
            .ProjectTo<DesignLogDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return logs;
    }
}
