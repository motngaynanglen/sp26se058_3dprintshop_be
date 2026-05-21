using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

public class GetDesignVariantListQuery : IRequest<List<DesignVariantDTO>>   
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid? DesignTemplateId { get; init; }
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid? MaterialId { get; init; }
    [DefaultValue(true)]
    public bool IsActive { get; init; } = true;
    [DefaultValue(CatalogStatuses.Published)]
    public string? CatalogStatus { get; init; }
}

public class GetDesignVariantListQueryHandler : IRequestHandler<GetDesignVariantListQuery, List<DesignVariantDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public GetDesignVariantListQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<List<DesignVariantDTO>> Handle(GetDesignVariantListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DesignVariants.AsNoTracking();
        // Áp dụng filter


        // 1. Áp dụng logic phân quyền cho IsActive

        bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;
        if (!isStaffOrManager)
        {
            // Khách hàng hoặc Guest luôn chỉ thấy hàng đang hoạt động
            query = query.Where(dv => dv.CatalogStatus == CatalogStatuses.Published && dv.IsActive
                && dv.DesignTemplate.CatalogStatus == CatalogStatuses.Published
                && dv.DesignTemplate.IsActive);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.CatalogStatus))
            {
                var status = request.CatalogStatus.ToUpperInvariant();
                if (!CatalogStatuses.IsValid(status))
                {
                    throw new BusinessException("Trạng thái catalog không hợp lệ.");
                }

                query = query.Where(dv => dv.CatalogStatus == status);
            }
            else
            {
                query = query.Where(dv => dv.IsActive == request.IsActive);
            }
        }

        // 2. Lọc theo Template
        if (request.DesignTemplateId.HasValue)
        {
            query = query.Where(dv => dv.DesignTemplateId == request.DesignTemplateId.Value);
        }

        // 3. Lọc theo Material
        if (request.MaterialId.HasValue)
        {
            query = query.Where(dv => dv.MaterialId == request.MaterialId.Value);
        }   

        // Sắp xếp (bạn có thể thay đổi theo nhu cầu)
        query = query.OrderBy(dv => dv.Code)
                     .ThenBy(dv => dv.Name);

        var variants = await query.ToListAsync(cancellationToken);
        var result = _mapper.Map<List<DesignVariantDTO>>(variants);

        return result;
    }
}
