using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
public class GetAccountMineDetailQuery : IRequest<AccountDTO>
{
    public class GetAccountMineDetailQueryHandler : IRequestHandler<GetAccountMineDetailQuery, AccountDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUser _user;

        public GetAccountMineDetailQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
        {
            _context = context;
            _mapper = mapper;
            _user = user;
        }
        public async Task<AccountDTO> Handle(GetAccountMineDetailQuery request, CancellationToken cancellationToken)
        {
            var userId = _user.Id.ToGuid();
            if (userId == Guid.Empty)
            {
                throw new Exception("Hãy đăng nhập.");
            }
            
            var query = _context.Accounts.AsNoTracking();
               
            var account = await query.ProjectTo<AccountDTO>(_mapper.ConfigurationProvider) //Mapper tự tính toán field Role
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (account == null) throw new Exception("Tài khoản không tồn tại");

            return account;
        }
    }
}
