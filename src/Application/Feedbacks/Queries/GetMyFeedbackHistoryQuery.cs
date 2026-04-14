using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
[Authorize(Roles = Roles.CUSTOMER)]
public class GetMyFeedbackHistoryQuery : PaginationRequest, IRequest<PaginatedList<FeedbackDTO>>
{
}
public class GetMyFeedbackHistoryQueryHandler : IRequestHandler<GetMyFeedbackHistoryQuery, PaginatedList<FeedbackDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetMyFeedbackHistoryQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<PaginatedList<FeedbackDTO>> Handle(GetMyFeedbackHistoryQuery request, CancellationToken ct)
    {
        var userId = _user.Id.ToGuid();
        // Lấy danh sách feedback dựa trên CustomerId của người đang đăng nhập
        return await _context.Feedbacks
            .AsNoTracking() // Luôn dùng cho API Query để tối ưu hiệu năng
            .Where(f => f.Customer.AccountId == userId)
            // Lưu ý: Nếu Bách không dùng Global Query Filter cho Deleted, hãy thêm .Where(f => f.Deleted == null)
            .OrderByDescending(f => f.Created)
            .ProjectTo<FeedbackDTO>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
