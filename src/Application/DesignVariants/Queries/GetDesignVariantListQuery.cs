using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

public class GetDesignVariantListQuery : IRequest<List<DesignVariantDTO>>
{
    public Guid? DesignTemplateId { get; init; }
    public Guid? MaterialId { get; init; }
    public Guid? ConceptTagId { get; init; }
    public bool IsActive { get; init; }
}

public class GetDesignVariantListQueryHandler : IRequestHandler<GetDesignVariantListQuery, List<DesignVariantDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDesignVariantListQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<DesignVariantDTO>> Handle(GetDesignVariantListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DesignVariants
            .IgnoreQueryFilters()
            .Include(dv => dv.DesignTemplate)
            .Include(dv => dv.Material)
            .AsNoTracking();

        if (request.DesignTemplateId.HasValue && request.DesignTemplateId.Value != Guid.Empty)
            query = query.Where(dv => dv.DesignTemplateId == request.DesignTemplateId.Value);

        if (request.MaterialId.HasValue && request.MaterialId.Value != Guid.Empty)
            query = query.Where(dv => dv.MaterialId == request.MaterialId.Value);

        if (request.ConceptTagId.HasValue && request.ConceptTagId.Value != Guid.Empty)
        {
            var tagId = request.ConceptTagId.Value;
            query = query.Where(dv =>
                dv.DesignTemplate.IsActive
                && dv.DesignTemplate.DesignTags.Any(dt =>
                    dt.IsActive && dt.ConceptTagId == tagId));
        }

        var scopedToTemplate = request.DesignTemplateId.HasValue && request.DesignTemplateId.Value != Guid.Empty;

        if (request.IsActive)
            query = query.Where(dv => dv.IsActive && dv.DesignTemplate.IsActive);
        else if (!scopedToTemplate)
            query = query.Where(dv => dv.DesignTemplate.IsActive);

        return await query
            .ProjectTo<DesignVariantDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
