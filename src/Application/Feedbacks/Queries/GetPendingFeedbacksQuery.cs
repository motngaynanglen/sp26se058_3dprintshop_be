using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Feedbacks;
using sp26se058_3dprintshop_be.Application.Common.Models;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
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
            var customerId = await FeedbackCustomerHelper.GetCurrentCustomerIdAsync(_context, _user, ct);
            var deliveredOrderIds = _context.Shipments
                .Where(s => s.ShipmentStatus == "DELIVERED")
                .Select(s => s.OrderId);

            return await _context.OrderItems
                .Where(oi => oi.Order.CustomerId == customerId
                    && (oi.Order.OrderStatus == "COMPLETED"
                        || oi.Order.CompletedAt != null
                        || deliveredOrderIds.Contains(oi.OrderId))
                    && !_context.Feedbacks.Any(f => f.OrderItemId == oi.Id))
                .ProjectTo<PendingFeedbackDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }
}
