using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.DesignVersions.Queries;

[Authorize(Roles = Roles.CustomerStaffManager)]
public record GetDesignVersionDetailQuery(Guid Id) : IRequest<DesignVersionHistoryDTO>;

public class GetDesignVersionDetailQueryHandler : IRequestHandler<GetDesignVersionDetailQuery, DesignVersionHistoryDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetDesignVersionDetailQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignVersionHistoryDTO> Handle(GetDesignVersionDetailQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DesignVersionHistorys
            .AsNoTracking()
            .Include(x => x.DesignWork)
            .Include(x => x.Uploader)
            .Where(x => x.Id == request.Id);

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

        var result = await query
            .ProjectTo<DesignVersionHistoryDTO>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            throw new DataNotFoundException(nameof(DesignVersionHistory), request.Id);
        }

        return result;
    }
}
