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
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;

public class GetDesignTemplatesWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<DesignTemplateDTO>>
{
    [DefaultValue("")]
    public string? Search { get; init; }
    [DefaultValue(false)]
    public bool SortDescending { get; init; } = false;
    [DefaultValue("Name")]
    public string? SortBy { get; init; }

    /// <summary>
    /// true = chỉ template CHƯA bị soft-delete (Deleted == null).
    /// false = bao gồm cả đã xóa mềm.
    /// null (default) = chỉ chưa xóa.
    /// </summary>
    [DefaultValue(null)]
    public bool? IncludeDeleted { get; init; }

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

            // Mặc định chỉ lấy chưa soft-delete
            if (request.IncludeDeleted != true)
            {
                query = query.Where(dt => !dt.Deleted.HasValue);
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(dt => dt.Name.Contains(request.Search) || dt.Code.Contains(request.Search));
            }

            bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;
            if (!isStaffOrManager)
            {
                // Customer/Guest chỉ thấy template có ít nhất 1 variant PUBLISHED + IsActive
                query = query.Where(dt =>
                    dt.Variants.Any(v => v.CatalogStatus == CatalogStatuses.Published && v.IsActive));
            }

            // Sắp xếp
            if (request.SortDescending)
            {
                query = request.SortBy?.ToLower() switch
                {
                    "name" => query.OrderByDescending(dt => dt.Name),
                    "code" => query.OrderByDescending(dt => dt.Code),
                    _ => query.OrderByDescending(dt => dt.Created)
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

            var count = await query.CountAsync(cancellationToken);
            var templates = await query
                .Include(x => x.Variants).ThenInclude(v => v.Material)
                .Include(x => x.DesignTags).ThenInclude(x => x.ConceptTag)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = _mapper.Map<List<DesignTemplateDTO>>(templates);
            var result = new PaginatedList<DesignTemplateDTO>(items, count, request.PageNumber, request.PageSize);

            if (!isStaffOrManager)
            {
                foreach (var item in result.Items)
                {
                    FilterCustomerChildren(item);
                }
            }

            return result;
        }

        private static void FilterCustomerChildren(DesignTemplateDTO designTemplate)
        {
            designTemplate.Variants = designTemplate.Variants
                .Where(x => x.CatalogStatus == CatalogStatuses.Published && x.IsActive)
                .ToList();
        }
    }
}
