using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
[Authorize(Roles =  Roles.MANAGER + "," + Roles.STAFF)]
public class GetFeedbacksWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<FeedbackDTO>>
{
    public int? Rating { get; init; }
    public bool? IsHidden { get; init; } // Filter xem các feedback bị ẩn
    public bool? IsDeleted { get; init; }
}
public class GetFeedbacksWithPaginationHandler : IRequestHandler<GetFeedbacksWithPaginationQuery, PaginatedList<FeedbackDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetFeedbacksWithPaginationHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<FeedbackDTO>> Handle(GetFeedbacksWithPaginationQuery request, CancellationToken ct)
    {
        var query = request.IsDeleted == true
            ? _context.Feedbacks.IgnoreQueryFilters().Where(x => x.Deleted != null)
            : _context.Feedbacks.AsNoTracking();
        // Filter theo số sao
        if (request.Rating.HasValue)
        {
            query = query.Where(x => x.Rating == request.Rating);
        }

        // Filter theo trạng thái ẩn/hiện
        if (request.IsHidden.HasValue)
        {
            query = query.Where(x => x.IsHidden == request.IsHidden);
        }
        return await query
            .OrderByDescending(x => x.Created)
            .ProjectTo<FeedbackDTO>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
