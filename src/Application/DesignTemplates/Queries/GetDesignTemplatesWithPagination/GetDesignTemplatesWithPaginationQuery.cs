using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;

public class GetDesignTemplatesWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<DesignTemplateDTO>>
{
    [DefaultValue("")]
    public string? Search { get; init; }
    public bool IsActive { get; init; } = true;
    public bool SortDescending { get; init; } = false;
    [DefaultValue("Name")]
    public string? SortBy { get; init; }
    public class GetDesignTemplatesWithPaginationQueryHandler : IRequestHandler<GetDesignTemplatesWithPaginationQuery, PaginatedList<DesignTemplateDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IUser _user;
        private readonly IMapper _mapper;
        public GetDesignTemplatesWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
        {
            _context = context;
            _mapper = mapper;
            _user = user;
        }
        public async Task<PaginatedList<DesignTemplateDTO>> Handle(GetDesignTemplatesWithPaginationQuery request, CancellationToken cancellationToken)
        {
            var query = _context.DesignTemplates.AsNoTracking();
            if(!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(dt => dt.Name.Contains(request.Search) || dt.Code.Contains(request.Search));
            }
            
            bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;
            if (!isStaffOrManager)
            {
                // Khách hàng hoặc Guest luôn chỉ thấy hàng đang hoạt động
                query = query.Where(dv => dv.IsActive);
            }
            else
            {
                // Đối với Staff/Manager, cho phép lọc theo IsActive từ request
                // Nếu request.IsActive là true thì lọc true, nếu false thì lọc false
                query = query.Where(dv => dv.IsActive == request.IsActive);
            }


            // Sắp xếp
            if (request.SortDescending)
                {
                query = request.SortBy?.ToLower() switch
                {
                    "Name" => query.OrderByDescending(dt => dt.Name),
                    "Code" => query.OrderByDescending(dt => dt.Code),
                    _ => query.OrderByDescending(dt => dt.Created)
                };
            }
            else
            {
                query = request.SortBy?.ToLower() switch
                {
                    "Created" => query.OrderBy(dt => dt.Created),
                    _ => query.OrderBy(dt => dt.Id)
                };
            }
            return await query
                .ProjectTo<DesignTemplateDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

        }
    }
}
