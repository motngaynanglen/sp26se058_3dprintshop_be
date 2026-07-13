using System.ComponentModel;

namespace sp26se058_3dprintshop_be.Application.Materials.Queries;

public class GetActiveMaterialsQuery : IRequest<IEnumerable<ActiveMaterialDTO>>
{
    [DefaultValue("")]
    public string? Search { get; init; }

    public class GetActiveMaterialsQueryHandler : IRequestHandler<GetActiveMaterialsQuery, IEnumerable<ActiveMaterialDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetActiveMaterialsQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ActiveMaterialDTO>> Handle(GetActiveMaterialsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Materials
                .AsNoTracking()
                .Where(x => x.IsActive && x.PriceHistories.Any(p => p.IsCurrent));

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(search) ||
                    (x.Description != null && x.Description.ToLower().Contains(search)));
            }

            return await query
                .OrderBy(x => x.Name)
                .ProjectTo<ActiveMaterialDTO>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }
    }
}
