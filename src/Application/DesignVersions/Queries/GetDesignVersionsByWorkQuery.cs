using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.DesignVersions.Queries;

[Authorize(Roles = Roles.CustomerStaffManager)]
public record GetDesignVersionsByWorkQuery(Guid DesignWorkId) : IRequest<List<DesignVersionHistoryDTO>>;

public class GetDesignVersionsByWorkQueryHandler : IRequestHandler<GetDesignVersionsByWorkQuery, List<DesignVersionHistoryDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetDesignVersionsByWorkQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<List<DesignVersionHistoryDTO>> Handle(GetDesignVersionsByWorkQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DesignVersionHistorys
            .AsNoTracking()
            .Include(x => x.DesignWork)
            .Include(x => x.Uploader)
            .Where(x => x.DesignWorkId == request.DesignWorkId);

        if (_user.Role == Roles.CUSTOMER)
        {
            var userId = _user.Id.ToGuid();
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AccountId == userId, cancellationToken);

            if (customer == null)
            {
                throw new ForbiddenAccessException();
            }

            query = query.Where(x => x.DesignWork.CustomerId == customer.Id);
        }

        return await query
            .OrderByDescending(x => x.VersionNumber)
            .ProjectTo<DesignVersionHistoryDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
