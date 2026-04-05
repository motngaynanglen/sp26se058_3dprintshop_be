using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.ServicePackages.Queries;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Queries;
public record GetServiceOptionsQuery : IRequest<List<ServiceOption>>;

public class GetServiceOptionsQueryHandler : IRequestHandler<GetServiceOptionsQuery, List<ServiceOption>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    public GetServiceOptionsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    } 

    public async Task<List<ServiceOption>> Handle(GetServiceOptionsQuery request, CancellationToken ct)
    {
        return await _context.ServiceOptions
            .AsNoTracking()
            .OrderBy(x => x.OptionType)
            .ProjectTo<ServiceOption>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}
