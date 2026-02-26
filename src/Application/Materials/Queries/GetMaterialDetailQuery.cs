using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.Materials.Queries;

public class GetMaterialDetailQuery : IRequest<MaterialDTO>
{
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
                .ProjectTo<MaterialDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
            if (material == null)
            {
                throw new Exception("Không tìm thấy chất liệu.");
            }
            return material;
        }
    }
}
