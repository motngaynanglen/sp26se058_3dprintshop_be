using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.TechnicalDrafts.Queries;

[Authorize(Roles = Roles.CustomerStaffManager)]
public record GetTechnicalDraftsByDesignWorkQuery(Guid DesignWorkId) : IRequest<List<TechnicalDraftDTO>>;

public class GetTechnicalDraftsByDesignWorkQueryHandler : IRequestHandler<GetTechnicalDraftsByDesignWorkQuery, List<TechnicalDraftDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetTechnicalDraftsByDesignWorkQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<List<TechnicalDraftDTO>> Handle(GetTechnicalDraftsByDesignWorkQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TechnicalDrafts
            .AsNoTracking()
            .Include(x => x.Material)
            .Include(x => x.DesignVersionHistory)
                .ThenInclude(x => x.DesignWork)
            .Where(x => x.DesignVersionHistory.DesignWorkId == request.DesignWorkId && x.Deleted == null);

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

            query = query.Where(x => x.DesignVersionHistory.DesignWork.CustomerId == customer.Id);
        }

        return await query
            .OrderByDescending(x => x.Created)
            .ProjectTo<TechnicalDraftDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
