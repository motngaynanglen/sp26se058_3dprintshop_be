using System.ComponentModel;

namespace sp26se058_3dprintshop_be.Application.Materials.Queries;

[Authorize(Roles = Roles.StaffOrManager)]
public class GetMaterialPriceHistoryQuery : PaginationRequest, IRequest<PaginatedList<MaterialPriceHistoryDTO>>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid MaterialId { get; init; }

    [DefaultValue(null)]
    public bool? IsCurrent { get; init; }

    public class GetMaterialPriceHistoryQueryHandler : IRequestHandler<GetMaterialPriceHistoryQuery, PaginatedList<MaterialPriceHistoryDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetMaterialPriceHistoryQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<MaterialPriceHistoryDTO>> Handle(GetMaterialPriceHistoryQuery request, CancellationToken cancellationToken)
        {
            var materialExists = await _context.Materials
                .AnyAsync(x => x.Id == request.MaterialId, cancellationToken);

            if (!materialExists)
            {
                throw new DataNotFoundException(nameof(Material), request.MaterialId);
            }

            var query = _context.MaterialPriceHistories
                .AsNoTracking()
                .Where(x => x.MaterialId == request.MaterialId);

            if (request.IsCurrent.HasValue)
            {
                query = query.Where(x => x.IsCurrent == request.IsCurrent.Value);
            }

            return await query
                .OrderByDescending(x => x.IsCurrent)
                .ThenByDescending(x => x.EffectiveDate)
                .ThenByDescending(x => x.Created)
                .ProjectTo<MaterialPriceHistoryDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }
}
