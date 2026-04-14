using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
[Authorize(Roles = Roles.CUSTOMER)]
public class GetPendingFeedbacksQuery : PaginationRequest, IRequest<PaginatedList<PendingFeedbackDTO>>
{
    public class GetPendingFeedbacksQueryHandler : IRequestHandler<GetPendingFeedbacksQuery, PaginatedList<PendingFeedbackDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IUser _user;
        private readonly IMapper _mapper;
        public GetPendingFeedbacksQueryHandler(IApplicationDbContext context, IUser user, IMapper mapper)
        {
            _context = context;
            _user = user;
            _mapper = mapper;
        }

        public async Task<PaginatedList<PendingFeedbackDTO>> Handle(GetPendingFeedbacksQuery request, CancellationToken ct)
        {
            var userId = _user.Id.ToGuid();
            var query = _context.OrderItems
                .AsNoTracking()
                .Where(oi => oi.Order.Customer.AccountId == userId
                        // 1. Order tổng phải hoàn thành
                        && oi.Order.OrderStatus == OrderStatuses.Completed
                        // 2. Quan trọng: Item đó phải thực sự đã giao/hoàn tất (tùy status của bạn)
                        // Nếu item vẫn PENDING thì chưa cho feedback
                        // && oi.FulfillmentStatus == FulfillmentStatuses.Fulfilled 

                        // 3. Item này chưa từng có feedback (Dùng !Any)
                        && !_context.Feedbacks.Any(f => f.OrderItemId == oi.Id));
            return await query
                .ProjectTo<PendingFeedbackDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }
}
