using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.TechnicalDrafts.Queries;

[Authorize(Roles = Roles.CUSTOMER)]
public class GetMyTechnicalDraftsQuery : PaginationRequest, IRequest<PaginatedList<TechnicalDraftDTO>>
{

    // Lọc theo dự án cụ thể nếu cần
    public Guid? DesignWorkId { get; init; }
}
public class GetMyTechnicalDraftsQueryHandler : IRequestHandler<GetMyTechnicalDraftsQuery, PaginatedList<TechnicalDraftDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetMyTechnicalDraftsQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<TechnicalDraftDTO>> Handle(GetMyTechnicalDraftsQuery request, CancellationToken ct)
    {
        var userId = _user.Id.ToGuid();

        // 1. Lấy Id của khách hàng từ AccountId
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AccountId == userId, ct);

        if (customer == null) throw new UnauthorizedAccessException();

        // 2. Query danh sách TechnicalDraft 
        // Logic: Draft -> VersionHistory -> DesignWork (phải thuộc CustomerId này)
        var query = _context.TechnicalDrafts
            .AsNoTracking()
            .Include(td => td.DesignVersionHistory)
                .ThenInclude(v => v.DesignWork)
            .Where(td => td.DesignVersionHistory.DesignWork.CustomerId == customer.Id);

        // 3. Lọc thêm theo DesignWorkId nếu khách hàng yêu cầu xem cụ thể 1 dự án
        if (request.DesignWorkId.HasValue)
        {
            query = query.Where(td => td.DesignVersionHistory.DesignWorkId == request.DesignWorkId.Value);
        }

        // 4. Sắp xếp mới nhất lên đầu và phân trang
        return await query
            .OrderByDescending(td => td.Created)
            .ProjectTo<TechnicalDraftDTO>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
