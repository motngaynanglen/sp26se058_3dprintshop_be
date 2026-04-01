using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignVariant.Queries;

namespace sp26se058_3dprintshop_be.Application.DesignVariant.Queries;

public class GetDesignVariantListQuery : IRequest<List<DesignVariantDTO>>   // ← Thay đổi ở đây
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid? DesignTemplateId { get; init; }
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid? MaterialId { get; init; }
    public bool IsActive { get; init; } = true;
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
        var query = _context.DesignVariants.AsNoTracking();

        // Áp dụng filter
        if (request.DesignTemplateId.HasValue)
        {
            query = query.Where(dv => dv.DesignTemplateId == request.DesignTemplateId.Value);
        }

        if (request.MaterialId.HasValue)
        {
            query = query.Where(dv => dv.MaterialId == request.MaterialId.Value);
        }

        if (request.IsActive)
        {
            query = query.Where(dv => dv.IsActive == true);
        }

        // Sắp xếp (bạn có thể thay đổi theo nhu cầu)
        query = query.OrderBy(dv => dv.Code)
                     .ThenBy(dv => dv.Name);

        // ProjectTo và lấy danh sách
        var result = await query
            .ProjectTo<DesignVariantDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return result;
    }
}
