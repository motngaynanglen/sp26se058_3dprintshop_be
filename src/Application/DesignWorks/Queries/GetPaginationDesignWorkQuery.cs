using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Queries;

public class GetPaginationDesignWorkQuery : PaginationRequest, IRequest<PaginatedList<DesignWorkDTO>>
{
    [DefaultValue("")]
    public string? Search { get; init; }
    [DefaultValue($"'{DesignWorkStatus.Sketching}', '{DesignWorkStatus.Pending}', '{DesignWorkStatus.InProgress}', '{DesignWorkStatus.Reviewing}', '{DesignWorkStatus.Completed}'")]
    public string Status { get; init; } = "";
    public bool SortDescending { get; init; } = false;
    [DefaultValue("Name")]
    public string? SortBy { get; init; }

    public class GetPaginationDesignWorkQueryHandler : IRequestHandler<GetPaginationDesignWorkQuery, PaginatedList<DesignWorkDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IUser _user;
        private readonly IMapper _mapper;
        public GetPaginationDesignWorkQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
        {
            _context = context;
            _mapper = mapper;
            _user = user;
        }
        public async Task<PaginatedList<DesignWorkDTO>> Handle(GetPaginationDesignWorkQuery request, CancellationToken cancellationToken)
        {
            var query = _context.DesignWorks.AsNoTracking();

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(dw => dw.Name != null && dw.Name.Contains(request.Search));
            }

            // 1. Kiểm tra quyền
            bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;

            if (!isStaffOrManager)
            {
                // 2. Lấy thông tin Customer dựa trên AccountId từ Identity User
                var userId = _user.Id; // Giả định _user.Id trả về string ID của Account

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.AccountId.ToString() == userId, cancellationToken);

                if (customer == null)
                {
                    // Nếu là Customer mà không tìm thấy thông tin trong bảng Customers thì trả về list trống
                    return new PaginatedList<DesignWorkDTO>(new List<DesignWorkDTO>(), 0, request.PageNumber, request.PageSize);
                }

                // 3. Lọc theo CustomerId (không phải AccountId)
                query = query.Where(dw => dw.CustomerId == customer.Id);
            }

            // Áp dụng lọc trạng thái nếu có
            if (!string.IsNullOrEmpty(request.Status))
            {
                var statusList = request.Status.Split(',').Select(s => s.Trim().Replace("'", ""));
                query = query.Where(dw => statusList.Contains(dw.Status));
            }

            // Áp dụng sắp xếp
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                query = request.SortDescending
                    ? query.OrderByDescending(e => EF.Property<object>(e, request.SortBy))
                    : query.OrderBy(e => EF.Property<object>(e, request.SortBy));
            }

            return await query
                .ProjectTo<DesignWorkDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }

    }
}
