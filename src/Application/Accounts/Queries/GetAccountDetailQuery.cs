using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
[Authorize(Roles = Roles.SystemAdmin + "," + Roles.MANAGER)]
public class GetAccountDetailQuery : IRequest<AccountDTO>
{
    public Guid Id { get; init; }
    public bool IncludeDeleted { get; init; } = false;

    public class GetAccountDetailQueryHandler : IRequestHandler<GetAccountDetailQuery, AccountDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUser _user;

        public GetAccountDetailQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
        {
            _context = context;
            _mapper = mapper;
            _user = user;
        }
        public async Task<AccountDTO> Handle(GetAccountDetailQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Accounts.AsNoTracking();
            if (request.IncludeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }

            if (_user.Role == Roles.MANAGER)
            {
                query = query.Where(x => x.Staff != null);
            }
               
            var account = await query.ProjectTo<AccountDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (account == null) throw new DataNotFoundException(nameof(Account), request.Id);

            return account;
        }
    }
}


