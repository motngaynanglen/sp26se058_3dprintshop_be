using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.TechnicalDrafts.Queries;

[Authorize(Roles = Roles.CustomerStaffManager)]
public record GetTechnicalDraftDetailQuery(Guid Id) : IRequest<TechnicalDraftDTO>;

public class GetTechnicalDraftDetailQueryHandler : IRequestHandler<GetTechnicalDraftDetailQuery, TechnicalDraftDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetTechnicalDraftDetailQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<TechnicalDraftDTO> Handle(GetTechnicalDraftDetailQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TechnicalDrafts
            .AsNoTracking()
            .Include(x => x.Material)
            .Include(x => x.DesignVersionHistory)
                .ThenInclude(x => x.DesignWork)
            .Where(x => x.Id == request.Id && x.Deleted == null);

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

        var result = await query
            .ProjectTo<TechnicalDraftDTO>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            throw new DataNotFoundException(nameof(TechnicalDraft), request.Id);
        }

        return result;
    }
}
