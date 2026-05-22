using System.ComponentModel;

namespace sp26se058_3dprintshop_be.Application.Materials.Queries;

[Authorize(Roles = Roles.StaffOrManager)]
public class GetMaterialsWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<MaterialDTO>>
{
    [DefaultValue("")]
    public string? Search { get; init; }

    [DefaultValue(null)]
    public bool? IsActive { get; init; }

    [DefaultValue(null)]
    public bool? HasCurrentPrice { get; init; }

    [DefaultValue("name")]
    public string? SortBy { get; init; } = "name";

    [DefaultValue(false)]
    public bool SortDescending { get; init; }

    public class GetMaterialsWithPaginationQueryHandler : IRequestHandler<GetMaterialsWithPaginationQuery, PaginatedList<MaterialDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetMaterialsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<MaterialDTO>> Handle(GetMaterialsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Materials
                .AsNoTracking()
                .Include(x => x.PriceHistories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(search) ||
                    (x.Description != null && x.Description.ToLower().Contains(search)));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            if (request.HasCurrentPrice.HasValue)
            {
                query = request.HasCurrentPrice.Value
                    ? query.Where(x => x.PriceHistories.Any(p => p.IsCurrent))
                    : query.Where(x => !x.PriceHistories.Any(p => p.IsCurrent));
            }

            query = ApplySort(query, request.SortBy, request.SortDescending);

            return await query
                .ProjectTo<MaterialDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }

        private static IQueryable<Material> ApplySort(IQueryable<Material> query, string? sortBy, bool descending)
        {
            var key = sortBy?.Trim().ToLowerInvariant();

            return key switch
            {
                "created" => descending ? query.OrderByDescending(x => x.Created) : query.OrderBy(x => x.Created),
                "price" => descending
                    ? query.OrderByDescending(x => x.PriceHistories.Where(p => p.IsCurrent).Select(p => p.TotalServiceCostPerGram).FirstOrDefault())
                    : query.OrderBy(x => x.PriceHistories.Where(p => p.IsCurrent).Select(p => p.TotalServiceCostPerGram).FirstOrDefault()),
                "effective-date" or "effectivedate" => descending
                    ? query.OrderByDescending(x => x.PriceHistories.Where(p => p.IsCurrent).Select(p => p.EffectiveDate).FirstOrDefault())
                    : query.OrderBy(x => x.PriceHistories.Where(p => p.IsCurrent).Select(p => p.EffectiveDate).FirstOrDefault()),
                _ => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name)
            };
        }
    }
}
