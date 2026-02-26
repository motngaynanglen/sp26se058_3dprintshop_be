using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.Orders.Queries;

public class GetOrderDetailQuery : IRequest<OrderDTO>
{
    public Guid Id { get; set; }

    public class GetOrderDetailQueryHandler : IRequestHandler<GetOrderDetailQuery, OrderDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        public GetOrderDetailQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<OrderDTO> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .ProjectTo<OrderDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
            if (order == null)
            {
                throw new Exception("Không tìm thấy đơn hàng.");
            }
            return order;
        }
    }
}
