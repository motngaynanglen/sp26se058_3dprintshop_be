using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Feedbacks;
using sp26se058_3dprintshop_be.Application.Common.Models;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
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
        var customerId = await FeedbackCustomerHelper.GetCurrentCustomerIdAsync(_context, _user, ct);
        return await _context.Feedbacks
            .Where(f => f.CustomerId == customerId)
            .ToPaginatedListAsync(_mapper, request.PageNumber, request.PageSize, ct);
    }
}
