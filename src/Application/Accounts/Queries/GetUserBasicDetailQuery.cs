using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Accounts.Queries;

[Authorize(Roles = Roles.StaffOrManager)]
public record GetUserBasicDetailQuery(Guid Id) : IRequest<UserBasicDTO>;

public class GetUserBasicDetailQueryHandler : IRequestHandler<GetUserBasicDetailQuery, UserBasicDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetUserBasicDetailQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<UserBasicDTO> Handle(GetUserBasicDetailQuery request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .Where(x => x.Id == request.Id && x.IsActive)
            .ProjectTo<UserBasicDTO>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
        {
            throw new DataNotFoundException(nameof(Account), request.Id);
        }

        return account;
    }
}
