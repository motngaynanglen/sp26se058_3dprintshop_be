using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Shipments.Queries;

[Authorize(Roles = Roles.CUSTOMER + ","+Roles.STAFF + "," + Roles.MANAGER)]

public class GetShipmentByIdQuery : IRequest<ShipmentDTO>
{
    public Guid Id { get; set; }
    public class GetShipmentByIdQueryHandler : IRequestHandler<GetShipmentByIdQuery, ShipmentDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUser _user;

        public GetShipmentByIdQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
        {
            _context = context;
            _mapper = mapper;
            _user = user;
        }

        public async Task<ShipmentDTO> Handle(GetShipmentByIdQuery request, CancellationToken cancellationToken)
        {
            // 1. Truy vấn Shipment kèm các thông tin liên quan
            var entity = await _context.Shipments
                .Include(x => x.ShippingAddress) 
                //.Include(x => x.ShippingMethod) //Tạm không dùng shipingmethod
                .Include(x => x.Order)
                    .ThenInclude(o=>o.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            // 2. Kiểm tra tồn tại
            if (entity == null)
            {
                throw new DataNotFoundException($"Không tìm thấy vận đơn với Id: {request.Id}");
            }

            // 3. Bảo mật: Nếu là Customer, chỉ cho phép xem vận đơn của chính họ
            // Giả sử Manager và Staff có quyền xem mọi vận đơn
            
            if (_user.Role == Roles.CUSTOMER)
            {
                var userId = _user.Id.ToGuid();
                if (entity.Order.Customer.AccountId != userId) 
                {
                    throw new ForbiddenAccessException("Bạn không có quyền xem vận đơn này!");
                }
            }
            
            // 4. Mapping sang DTO (Sử dụng cấu hình Mapping Profile đã tạo trước đó)
            return _mapper.Map<ShipmentDTO>(entity);
        }
    }
}
