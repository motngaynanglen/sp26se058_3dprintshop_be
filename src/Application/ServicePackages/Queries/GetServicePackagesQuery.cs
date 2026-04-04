using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.ServicePackages.Queries;
public record GetServicePackagesQuery : IRequest<List<ServicePackageDTO>>
{
    [DefaultValue("")]
    public string? search { get; set; }
    [DefaultValue("DESIGN Hoặc PRINTING")]
    public string? service { get; set; }
    [DefaultValue("Created")]
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = false;
}
public class GetServicePackagesQueryHandler : IRequestHandler<GetServicePackagesQuery, List<ServicePackageDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetServicePackagesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<ServicePackageDTO>> Handle(GetServicePackagesQuery request, CancellationToken ct)
    {
        var query = _context.ServicePackages
            .Include(p => p.PackageOptions)
                .ThenInclude(po => po.ServiceOption)
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (!string.IsNullOrEmpty(request.search))
        {
            query = query.Where(s => s.Name.Contains(request.search) || s.Code.Contains(request.search));
        }
        if (!string.IsNullOrEmpty(request.service))
        {
            query = query.Where(s => s.ServiceType.Contains(request.service));
        }

        query = request.SortBy switch
        {
            "Name" => request.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "Code" => request.SortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "Created" => request.SortDescending ? query.OrderByDescending(x => x.Created) : query.OrderBy(x => x.Created),
            _ => query.OrderBy(x => x.Created) // Mặc định theo Created
        };

        return await query
                    .ProjectTo<ServicePackageDTO>(_mapper.ConfigurationProvider)
                    .ToListAsync(ct);
    }
}
