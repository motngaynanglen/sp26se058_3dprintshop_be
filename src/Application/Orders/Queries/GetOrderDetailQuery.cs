using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Orders.Queries;

[Authorize(Roles = Roles.MANAGER + "," + Roles.STAFF + "," + Roles.CUSTOMER)]

public record GetOrderDetailQuery : IRequest<OrderDTO>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; set; }
}
public class GetOrderDetailQueryHandler : IRequestHandler<GetOrderDetailQuery, OrderDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user; 
    public GetOrderDetailQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }
    public async Task<OrderDTO> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Orders
                .Include(o => o.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        if (entity == null)
        {
            throw new DataNotFoundException(nameof(Order), request.Id);
        }
        if (_user.Role == Roles.CUSTOMER)
        {
            var userId = _user.Id.ToGuid();
            // Nếu AccountId của chủ đơn hàng không khớp với User đang đăng nhập
            if (entity.Customer.AccountId != userId)
            {
                throw new ForbiddenAccessException("Bạn không có quyền xem chi tiết đơn hàng này.");
            }
        }
        return _mapper.Map<OrderDTO>(entity);
    }
}

