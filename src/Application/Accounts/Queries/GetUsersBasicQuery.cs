using System.ComponentModel;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Accounts.Queries;

[Authorize(Roles = Roles.StaffOrManager)]
public class GetUsersBasicQuery : PaginationRequest, IRequest<PaginatedList<UserBasicDTO>>
{
    [DefaultValue("CUSTOMER")]
    public string? Role { get; init; }

    [DefaultValue("Nguyen van a")]
    public string? Search { get; init; }

    public bool SortDescending { get; init; } = false;

    [DefaultValue("Name")]
    public string? SortBy { get; init; }

    public class Handler : IRequestHandler<GetUsersBasicQuery, PaginatedList<UserBasicDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public Handler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<UserBasicDTO>> Handle(GetUsersBasicQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Accounts
                .AsNoTracking()
                .Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Username.ToLower().Contains(search) ||
                    x.Fullname.ToLower().Contains(search) ||
                    x.Email.ToLower().Contains(search) ||
                    (x.ContactPhone != null && x.ContactPhone.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                query = request.Role.Trim().ToUpper() switch
                {
                    Roles.MANAGER => query.Where(x => x.Manager != null),
                    Roles.STAFF => query.Where(x => x.Staff != null),
                    Roles.CUSTOMER => query.Where(x => x.Customer != null),
                    _ => query
                };
            }

            query = request.SortBy switch
            {
                "Name" => request.SortDescending ? query.OrderByDescending(x => x.Fullname) : query.OrderBy(x => x.Fullname),
                "Email" => request.SortDescending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
                "Phone" => request.SortDescending ? query.OrderByDescending(x => x.ContactPhone) : query.OrderBy(x => x.ContactPhone),
                "Created" => request.SortDescending ? query.OrderByDescending(x => x.Created) : query.OrderBy(x => x.Created),
                _ => query.OrderBy(x => x.Fullname)
            };

            return await query
                .ProjectTo<UserBasicDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }
}
