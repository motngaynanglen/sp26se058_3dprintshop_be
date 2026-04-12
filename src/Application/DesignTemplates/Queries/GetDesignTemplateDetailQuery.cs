using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;
using sp26se058_3dprintshop_be.Domain.Constants;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Queries;

public record GetDesignTemplateDetailQuery : IRequest<DesignTemplateDTO>
{
    [JsonIgnore]
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
            // Khách hàng hoặc Guest luôn chỉ thấy hàng đang hoạt động
            query = query.Where(dv => dv.IsActive);
        }


        var designTemplate = await query
            .ProjectTo<DesignTemplateDTO>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(dt => dt.Id == request.Id, cancellationToken);
        if (designTemplate == null)
        {
            throw new DataNotFoundException("Không tìm thấy mẫu thiết kế.");
        }
        return designTemplate;
    }
}

