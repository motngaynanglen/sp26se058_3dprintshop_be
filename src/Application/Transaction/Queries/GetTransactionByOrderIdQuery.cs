using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Transaction.Queries;
public class GetTransactionByOrderIdQuery : IRequest<TransactionDTO>
{
    [Required]
    public Guid OrderId { get; set; }
    public class GetTransactionByOrderIdQueryHandler : IRequestHandler<GetTransactionByOrderIdQuery, TransactionDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IUser _user;
        private readonly IMapper _mapper;

        public GetTransactionByOrderIdQueryHandler(IApplicationDbContext context, IUser user, IMapper mapper)
        {
            _context = context;
            _user = user;
            _mapper = mapper;
        }
        public async Task<TransactionDTO> Handle(GetTransactionByOrderIdQuery request, CancellationToken cancellationToken)
        {
            var userId = _user.Id.ToGuid();

            var query = _context.Transactions
                .Include(t => t.Invoice).ThenInclude(i => i.Order)
                .AsNoTracking()
                .Where(t => t.Invoice.OrderId == request.OrderId);

            if (_user.Role == Roles.CUSTOMER)
            {
                query = query.Where(t => t.Invoice.Order.CustomerId == userId);
            }
            var entity = await query
                .ProjectTo<TransactionDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new NotFoundException(nameof(Domain.Entities.Transaction), request.OrderId.ToString());
            }

            return entity;


        }
    }
}
