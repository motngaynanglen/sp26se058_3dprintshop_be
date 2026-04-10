using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.ServicePackages.Queries;
public record GetServicePackageByIdQuery : IRequest<List<ServicePackageDTO>>
{
    [DefaultValue("guid")]
    public Guid Id { get; init; }
    
}
public class GetServicePackageByIdQueryHandler : IRequestHandler<GetServicePackageByIdQuery, List<ServicePackageDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetServicePackageByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<ServicePackageDTO>> Handle(GetServicePackageByIdQuery request, CancellationToken ct)
    {
        var query = _context.ServicePackages
            .Include(p => p.PackageOptions)
                .ThenInclude(po => po.ServiceOption)
            .AsNoTracking();

        return await query
                    .ProjectTo<ServicePackageDTO>(_mapper.ConfigurationProvider)
                    .ToListAsync(ct);
    }
}
