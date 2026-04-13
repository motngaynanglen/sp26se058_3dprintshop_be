using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Queries;
public record GetServiceOptionsQuery : IRequest<List<ServiceOptionDTO>>;

public class GetServiceOptionsQueryHandler : IRequestHandler<GetServiceOptionsQuery, List<ServiceOptionDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    public GetServiceOptionsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    } 

    public async Task<List<ServiceOptionDTO>> Handle(GetServiceOptionsQuery request, CancellationToken ct)
    {
        return await _context.ServiceOptions
            .AsNoTracking()
            .OrderBy(x => x.Created)
            .ProjectTo<ServiceOptionDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}
