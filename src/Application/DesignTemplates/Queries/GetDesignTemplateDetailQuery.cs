using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Queries;

public record GetDesignTemplateDetailQuery : IRequest<DesignTemplateDTO>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid Id { get; init; }
}
public class GetDesignTemplateDetailQueryHandler : IRequestHandler<GetDesignTemplateDetailQuery, DesignTemplateDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;
    public GetDesignTemplateDetailQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }
    public async Task<DesignTemplateDTO> Handle(GetDesignTemplateDetailQuery request, CancellationToken cancellationToken)
    {

        var query = _context.DesignTemplates.AsNoTracking();

        bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;
        if (!isStaffOrManager)
        {
            // Customer/Guest chỉ thấy template có ít nhất 1 variant PUBLISHED
            query = query.Where(dt =>
                dt.Variants.Any(v => v.CatalogStatus == CatalogStatuses.Published && v.IsActive));
        }


        var designTemplate = await query
            .Include(x => x.Variants)
            .Include(x => x.DesignTags).ThenInclude(x => x.ConceptTag)
            .FirstOrDefaultAsync(dt => dt.Id == request.Id, cancellationToken);
        if (designTemplate == null)
        {
            throw new DataNotFoundException("Không tìm thấy mẫu thiết kế.");
        }
        var dto = _mapper.Map<DesignTemplateDTO>(designTemplate);
        if (!isStaffOrManager)
        {
            FilterCustomerChildren(dto);
        }
        return dto;
    }

    private static void FilterCustomerChildren(DesignTemplateDTO designTemplate)
    {
        designTemplate.Variants = designTemplate.Variants
            .Where(x => x.CatalogStatus == CatalogStatuses.Published && x.IsActive)
            .ToList();
    }
}

