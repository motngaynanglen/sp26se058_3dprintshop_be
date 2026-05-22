using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.Materials.Queries;

[Authorize(Roles = Roles.StaffOrManager)]
public class GetMaterialListQuery : IRequest<IEnumerable<MaterialDTO>>
{
    public class GetMaterialListQueryHandler : IRequestHandler<GetMaterialListQuery, IEnumerable<MaterialDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        public GetMaterialListQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<IEnumerable<MaterialDTO>> Handle(GetMaterialListQuery request, CancellationToken cancellationToken)
        {
            var materials = await _context.Materials
                .AsNoTracking()
                .Include(m => m.PriceHistories)
                .OrderBy(m => m.Name)
                .ProjectTo<MaterialDTO>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
            return materials;
        }
    }
}
