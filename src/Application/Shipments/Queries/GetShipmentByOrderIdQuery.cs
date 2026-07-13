using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Shipments.Queries;
[Authorize(Roles = Roles.CUSTOMER + "," + Roles.STAFF + "," + Roles.MANAGER)]
public class GetShipmentByOrderIdQuery : IRequest<ShipmentDTO>
{
    public Guid OrderId { get; set; }

    public class GetShipmentByOrderIdQueryHandler : IRequestHandler<GetShipmentByOrderIdQuery, ShipmentDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUser _user;
        public GetShipmentByOrderIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
        {
            _context = context;
            _mapper = mapper;
            _user = user;
        }
        public async Task<ShipmentDTO> Handle(GetShipmentByOrderIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _context.Shipments
                .Include(s => s.ShippingAddress)
                .Include(s => s.Order)
                    .ThenInclude(o => o.Customer)
                .AsNoTracking() // Sử dụng AsNoTracking để tối ưu cho Query
                .FirstOrDefaultAsync(x => x.OrderId == request.OrderId, cancellationToken);
            if (entity == null)
            {
                throw new DataNotFoundException("Đơn hàng không có thông tin vận chuyển.");
            }
            if (_user.Role == Roles.CUSTOMER)
            {
                var userId = _user.Id.ToGuid();
                if (entity.Order.Customer.AccountId != userId)
                {
                    throw new ForbiddenAccessException("Bạn không có quyền xem vận đơn của đơn hàng này!");
                }
            }

            return _mapper.Map<ShipmentDTO>(entity);
        }
    }
}
