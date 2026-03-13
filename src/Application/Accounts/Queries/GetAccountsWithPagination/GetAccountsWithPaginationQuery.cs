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
[Authorize(Roles = Roles.ADMIN)]
public class GetAccountsWithPaginationQuery : IRequest<PaginatedList<AccountDto>>
{
    [DefaultValue("CUSTOMER")]
    public string? Role { get; init; }
    [DefaultValue("Nguyen van a")]
    public string? Search { get; init; }

    // Sắp xếp
    [DefaultValue("Name")]
    public string? SortBy { get; init; } // "Name", "Phone", "Created", "Deleted"
    public bool SortDescending { get; init; } = false;
    // Có dữ liệu xóa mềm
    public bool IncludeDeleted { get; init; } = false;
    // Paging
    public PaginationData Paging { get; init; } = new();
    public class PaginationData : PaginationRequest { }

    public class GetAccountsWithPaginationQueryHandler : IRequestHandler<GetAccountsWithPaginationQuery, PaginatedList<AccountDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetAccountsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<AccountDto>> Handle(GetAccountsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Accounts.AsNoTracking();
            if (request.IncludeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }
            // 1. Search theo thông tin cơ bản trong bảng Account
            if (!string.IsNullOrEmpty(request.Search))
            {
                var s = request.Search.ToLower();
                query = query.Where(x =>
                    x.Username.ToLower().Contains(s) ||
                    x.Fullname.ToLower().Contains(s) ||
                    x.Email.ToLower().Contains(s));
            }
            // 2. Filter theo Role (Dựa trên quan hệ 1-1)
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
            // 3. sắp xếp
            query = request.SortBy switch
            {
                "Name" => request.SortDescending ? query.OrderByDescending(x => x.Fullname) : query.OrderBy(x => x.Fullname),
                "Email" => request.SortDescending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
                "Phone" => request.SortDescending ? query.OrderByDescending(x => x.ContactPhone) : query.OrderBy(x => x.ContactPhone),
                "Created" => request.SortDescending ? query.OrderByDescending(x => x.Created) : query.OrderBy(x => x.Created),
                "Deleted" => request.SortDescending ? query.OrderByDescending(x => x.Deleted) : query.OrderBy(x => x.Deleted),
                _ => query.OrderBy(x => x.Username) // Mặc định theo Username
            };

            return await query
                .ProjectTo<AccountDto>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.Paging.PageNumber, request.Paging.PageSize);
        }
    }
}
