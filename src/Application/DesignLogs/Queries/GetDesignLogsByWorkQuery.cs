using System.ComponentModel;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.DesignLogs.Queries;

[Authorize(Roles = Roles.CustomerStaffManager)]
public class GetDesignLogsByWorkQuery : PaginationRequest, IRequest<PaginatedList<DesignLogDTO>>
{
    public Guid DesignWorkId { get; init; }
    public Guid? BeforeLogId { get; init; }

    [DefaultValue(30)]
    public new int PageSize
    {
        get => base.PageSize;
        init => base.PageSize = value;
    }
}

public class GetDesignLogsByWorkQueryHandler : IRequestHandler<GetDesignLogsByWorkQuery, PaginatedList<DesignLogDTO>>
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

    public async Task<PaginatedList<DesignLogDTO>> Handle(GetDesignLogsByWorkQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DesignLogs
            .AsNoTracking()
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
            query = query.Where(l => l.LogType != DesignLogType.InternalNote);
        }

        var pageSize = request.PageSize;

        if (request.BeforeLogId.HasValue)
        {
            var cursor = await query
                .Where(l => l.Id == request.BeforeLogId.Value)
                .Select(l => new { l.Id, l.Created })
                .FirstOrDefaultAsync(cancellationToken);

            if (cursor == null)
            {
                throw new DataNotFoundException(nameof(DesignLog), request.BeforeLogId.Value);
            }

            query = query.Where(l => l.Created < cursor.Created);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var latestFirstItems = await query
            .OrderByDescending(l => l.Created)
            .ThenByDescending(l => l.Id)
            .Skip(request.BeforeLogId.HasValue ? 0 : (request.PageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<DesignLogDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        var chronologicalItems = latestFirstItems
            .OrderBy(x => x.Created)
            .ThenBy(x => x.Id)
            .ToList();

        return new PaginatedList<DesignLogDTO>(
            chronologicalItems,
            totalCount,
            request.BeforeLogId.HasValue ? 1 : request.PageNumber,
            pageSize);
    }
}
