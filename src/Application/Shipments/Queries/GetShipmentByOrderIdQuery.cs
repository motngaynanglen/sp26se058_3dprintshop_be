using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Materials.Queries;

namespace sp26se058_3dprintshop_be.Application.Shipments.Queries;
public class GetShipmentByOrderIdQuery : IRequest<ShipmentDTO>
{
    public Guid OrderId { get; set; }

    public class GetShipmentByOrderIdQueryHandler : IRequestHandler<GetShipmentByOrderIdQuery, ShipmentDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        public GetShipmentByOrderIdQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        } 
        public async Task<ShipmentDTO> Handle(GetShipmentByOrderIdQuery request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                //.Include(s => s.ShippingMethod)
                .Include(s => s.ShippingAddress)
                .Where(x => x.OrderId == request.OrderId)
                .ProjectTo<ShipmentDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (shipment == null) throw new DataNotFoundException("Đơn hàng không có đơn vận chuyển.");
            return shipment;
        }
    }
}
