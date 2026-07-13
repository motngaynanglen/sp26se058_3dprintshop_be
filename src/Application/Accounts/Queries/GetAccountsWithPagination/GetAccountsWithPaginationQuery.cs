using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
[Authorize(Roles = Roles.SystemAdmin + "," + Roles.MANAGER)]
public class GetAccountsWithPaginationQuery : IRequest<PaginatedList<AccountDTO>>
{
    [DefaultValue("CUSTOMER")]
    public string? Role { get; init; }
    [DefaultValue("Nguyen van a")]
    public string? Search { get; init; }

    // Sorting
    [DefaultValue("Name")]
    public string? SortBy { get; init; } // "Name", "Phone", "Created", "Deleted"
    public bool SortDescending { get; init; } = false;
    // Include soft-deleted records
    public bool IncludeDeleted { get; init; } = false;
    // Paging
    public PaginationData Paging { get; init; } = new();
    public class PaginationData : PaginationRequest { }

    public class GetAccountsWithPaginationQueryHandler : IRequestHandler<GetAccountsWithPaginationQuery, PaginatedList<AccountDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUser _user;

        public GetAccountsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
        {
            _context = context;
            _mapper = mapper;
            _user = user;
        }

        public async Task<PaginatedList<AccountDTO>> Handle(GetAccountsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Accounts.AsNoTracking();
            if (request.IncludeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }

            if (_user.Role == Roles.MANAGER)
            {
                query = query.Where(x => x.Staff != null);
            }
            // 1. Search by basic account information.
            if (!string.IsNullOrEmpty(request.Search))
            {
                var s = request.Search.ToLower();
                query = query.Where(x =>
                    x.Username.ToLower().Contains(s) ||
                    x.Fullname.ToLower().Contains(s) ||
                    x.Email.ToLower().Contains(s));
            }
            // 2. Filter by role based on 1-1 relationships.
            if (!string.IsNullOrEmpty(request.Role))
            {
                query = request.Role.ToUpper() switch
                {
                    Roles.MANAGER => query.Where(x => x.Manager != null),
                    Roles.STAFF => query.Where(x => x.Staff != null),
                    Roles.CUSTOMER => query.Where(x => x.Customer != null),
                    _ => query
                };
            }
            // 3. Sort.
            query = request.SortBy switch
            {
                "Name" => request.SortDescending ? query.OrderByDescending(x => x.Fullname) : query.OrderBy(x => x.Fullname),
                "Email" => request.SortDescending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
                "Phone" => request.SortDescending ? query.OrderByDescending(x => x.ContactPhone) : query.OrderBy(x => x.ContactPhone),
                "Created" => request.SortDescending ? query.OrderByDescending(x => x.Created) : query.OrderBy(x => x.Created),
                "Deleted" => request.SortDescending ? query.OrderByDescending(x => x.Deleted) : query.OrderBy(x => x.Deleted),
                _ => query.OrderBy(x => x.Username)
            };

            return await query
                .ProjectTo<AccountDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.Paging.PageNumber, request.Paging.PageSize);
        }
    }
}


