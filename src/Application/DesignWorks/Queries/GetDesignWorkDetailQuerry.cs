using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Queries;

[Authorize(Roles = Roles.CustomerStaffManager)]
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
        if (!isStaffOrManager)
        {
            var userId = _user.Id.ToGuid();
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AccountId == userId, cancellationToken);

            if (customer == null)
            {
                throw new ForbiddenAccessException();
            }

            query = query.Where(dv => dv.CustomerId.Equals(customer.Id));
        }
        var designWork = await query.FirstOrDefaultAsync(dv => dv.Id.Equals(request.Id), cancellationToken);

        if (designWork == null) {
            throw new DataNotFoundException("Không tìm thấy công việc thiết kế.");
        }
        return _mapper.Map<DesignWorkDTO>(designWork);
    }
}
