using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;

public class GetDesignTemplatesWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<DesignTemplateDTO>>
{
    public string? Search { get; init; }
    public bool IsActive { get; init; } = true;
    public bool SortDescending { get; init; } = false;
    public string? SortBy { get; init; }
    public class GetDesignTemplatesWithPaginationQueryHandler : IRequestHandler<GetDesignTemplatesWithPaginationQuery, PaginatedList<DesignTemplateDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        public GetDesignTemplatesWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<PaginatedList<DesignTemplateDTO>> Handle(GetDesignTemplatesWithPaginationQuery request, CancellationToken cancellationToken)
        {
            var query = _context.DesignTemplates.AsNoTracking();
            if(!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(dt => dt.Name.Contains(request.Search) || dt.Code.Contains(request.Search));
            }
            if (request.IsActive)
            {
                query = query.Where(dt => dt.IsActive);
            }

            // Sắp xếp
            if (request.SortDescending)
                {
                query = request.SortBy?.ToLower() switch
                {
                    "name" => query.OrderByDescending(dt => dt.Name),
                    "code" => query.OrderByDescending(dt => dt.Code),
                    _ => query.OrderByDescending(dt => dt.Id)
                };
            }
            else
            {
                query = request.SortBy?.ToLower() switch
                {
                    "created" => query.OrderBy(dt => dt.Created),
                    _ => query.OrderBy(dt => dt.Id)
                };
            }
            return await query
                .ProjectTo<DesignTemplateDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

            //return await PaginatedList<DesignTemplateDTO>.CreateAsync(
            //    query.ProjectTo<DesignTemplateDTO>(_mapper.ConfigurationProvider),
            //    request.PageNumber,
            //    request.PageSize,
            //    cancellationToken);
        }
    }
}
