using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

public record GetDesignVariantByIdQuery : IRequest<DesignVariantDTO?>
{
    public Guid Id { get; init; }
}

public class GetDesignVariantByIdQueryHandler : IRequestHandler<GetDesignVariantByIdQuery, DesignVariantDTO?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDesignVariantByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<DesignVariantDTO?> Handle(GetDesignVariantByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.DesignVariants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(dv => dv.DesignTemplate)
            .Include(dv => dv.Material)
            .Where(dv => dv.Id == request.Id)
            .ProjectTo<DesignVariantDTO>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
