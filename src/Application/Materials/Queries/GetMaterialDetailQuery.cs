using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.Materials.Queries;

[Authorize(Roles = Roles.StaffOrManager)]
public class GetMaterialDetailQuery : IRequest<MaterialDTO>
{
    [System.ComponentModel.DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid Id { get; init; }
    public class GetMaterialDetailQueryHandler : IRequestHandler<GetMaterialDetailQuery, MaterialDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        public GetMaterialDetailQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<MaterialDTO> Handle(GetMaterialDetailQuery request, CancellationToken cancellationToken)
        {
            var material = await _context.Materials
                .AsNoTracking()
                .Include(m => m.PriceHistories)
                .ProjectTo<MaterialDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
            if (material == null)
            {
                throw new DataNotFoundException(nameof(Material), request.Id);
            }
            return material;
        }
    }
}
