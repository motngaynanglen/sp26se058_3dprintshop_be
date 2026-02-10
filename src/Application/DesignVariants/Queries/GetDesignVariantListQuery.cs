using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;

namespace sp26se058_3dprintshop_be.Application.DesignVariant.Queries;

public class GetDesignVariantListQuery : PaginationRequest, IRequest<PaginatedList<DesignVariantDTO>>
{
    public string? Search { get; init; }
    public Guid? DesignTemplateId { get; init; }
    public Guid? MaterialId { get; init; }
    public bool IsActive { get; init; } = true;
    
    public class GetDesignVariantListQueryHandler : IRequestHandler<GetDesignVariantListQuery, PaginatedList<DesignVariantDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        public GetDesignVariantListQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<PaginatedList<DesignVariantDTO>> Handle(GetDesignVariantListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.DesignVariants.AsNoTracking();
            if(!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(dv => dv.Name.Contains(request.Search) || dv.Code.Contains(request.Search));
            }
            if (request.DesignTemplateId.HasValue)
            {
                query = query.Where(dv => dv.DesignTemplateId == request.DesignTemplateId);
            }
            if (request.MaterialId.HasValue)
            {
                query = query.Where(dv => dv.MaterialId == request.MaterialId);
            }
            if (request.IsActive)
            {
                query = query.Where(dv => dv.IsActive);
            }
            // Sắp xếp
            query = query.OrderBy(dv => dv.Id);
            return await query
                .ProjectTo<DesignVariantDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }
}
