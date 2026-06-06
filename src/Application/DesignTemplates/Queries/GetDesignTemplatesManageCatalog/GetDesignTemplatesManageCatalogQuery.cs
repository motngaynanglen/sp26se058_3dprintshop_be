using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesManageCatalog;

public class GetDesignTemplatesManageCatalogQuery : PaginationRequest, IRequest<PaginatedList<DesignTemplateManageCatalogItemDto>>
{
    public string? Search { get; init; }
    /// <summary>false = chỉ mẫu đang active; true = gồm cả mẫu đã tắt (mặc định cho manager).</summary>
    public bool IncludeInactive { get; init; } = true;
    public Guid? ConceptTagId { get; init; }
}

public class GetDesignTemplatesManageCatalogQueryHandler
    : IRequestHandler<GetDesignTemplatesManageCatalogQuery, PaginatedList<DesignTemplateManageCatalogItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDesignTemplatesManageCatalogQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<DesignTemplateManageCatalogItemDto>> Handle(
        GetDesignTemplatesManageCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.DesignTemplates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(dt => dt.Name.Contains(term) || dt.Code.Contains(term));
        }

        if (!request.IncludeInactive)
            query = query.Where(dt => dt.IsActive);

        if (request.ConceptTagId.HasValue && request.ConceptTagId.Value != Guid.Empty)
        {
            var tagId = request.ConceptTagId.Value;
            query = query.Where(dt =>
                dt.DesignTags.Any(t => t.IsActive && t.ConceptTagId == tagId));
        }

        query = query.OrderByDescending(dt => dt.Created);

        var paged = await query
            .Select(dt => new DesignTemplateManageCatalogItemDto
            {
                Id = dt.Id,
                Code = dt.Code,
                Name = dt.Name,
                Description = dt.Description,
                FileUrl = dt.FileUrl,
                ThumbnailUrl = dt.ThumbnailUrl,
                IsActive = dt.IsActive,
                Created = dt.Created,
                VariantCount = dt.Variants.Count,
                ActiveVariantCount = dt.Variants.Count(v => v.IsActive),
                ConceptTagNames = dt.DesignTags
                    .Where(t => t.IsActive)
                    .Select(t => t.ConceptTag.Name)
                    .ToList()
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize);

        if (paged.Items.Count == 0)
            return paged;

        var templateIds = paged.Items.Select(t => t.Id).ToList();
        var variants = await _context.DesignVariants
            .AsNoTracking()
            .Where(v => templateIds.Contains(v.DesignTemplateId))
            .OrderBy(v => v.Code)
            .ProjectTo<DesignVariantDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        var byTemplate = variants.GroupBy(v => v.DesignTemplateId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var item in paged.Items)
        {
            if (byTemplate.TryGetValue(item.Id, out var list))
                item.Variants = list;
        }

        return paged;
    }
}
