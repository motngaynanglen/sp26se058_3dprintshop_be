using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.ShippingAddress.Queries;
public class GetMyShippingAddressesQuery : IRequest<List<ShippingAddressDTO>>
{


    public class GetMyShippingAddressesHandler : IRequestHandler<GetMyShippingAddressesQuery, List<ShippingAddressDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IUser _user; 
        private readonly IMapper _mapper;

        public GetMyShippingAddressesHandler(IApplicationDbContext context, IUser user, IMapper mapper)
        {
            _context = context;
            _user = user;
            _mapper = mapper;
        }

        public async Task<List<ShippingAddressDTO>> Handle(GetMyShippingAddressesQuery request, CancellationToken cancellationToken)
        {
            var userId = _user.Id.ToGuid();
            return await _context.ShippingAddresses
                .Where(s => s.CustomerId == userId)
                .ProjectTo<ShippingAddressDTO>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }
    }
}
