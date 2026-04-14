using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Queries;

public record GetDesignWorkDetailQuerry : IRequest<DesignWorkDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }
}

public class GetDesignWorkDetailQuerryHandler : IRequestHandler<GetDesignWorkDetailQuerry, DesignWorkDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;
    public GetDesignWorkDetailQuerryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }
    public async Task<DesignWorkDTO> Handle(GetDesignWorkDetailQuerry request, CancellationToken cancellationToken)
    {
        var query = _context.DesignWorks.AsNoTracking();
        bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;
        Customer customer = (Customer)_context.Customers.Where(c => c.Account.Id.Equals(_user.Id));
        if (!isStaffOrManager)
        {
            // Khách hàng hoặc Guest luôn chỉ thấy hàng đang hoạt động
            
            query = query.Where(dv => dv.CustomerId.Equals(customer.Id));
        }
        var designWork = await query.FirstOrDefaultAsync(dv => dv.Id.Equals(request.Id), cancellationToken);

        if (designWork == null) { 
            throw new DataNotFoundException("DesignWork not found");
        }
        return _mapper.Map<DesignWorkDTO>(designWork);
    }
}
