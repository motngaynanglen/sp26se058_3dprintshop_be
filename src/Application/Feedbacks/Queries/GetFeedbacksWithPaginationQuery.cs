using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;

[Authorize(Roles = $"{Roles.STAFF},{Roles.MANAGER},{Roles.ADMIN}")]
public class GetFeedbacksWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<FeedbackDTO>>
{
    public string? Search { get; init; }
    public int? Rating { get; init; }
    public bool? IsHidden { get; init; }
    public bool? HasStaffReply { get; init; }
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
        var query = _context.Feedbacks.AsQueryable();
        if (request.IsDeleted.HasValue)
            query = query.IgnoreQueryFilters();

        if (request.Rating.HasValue)
            query = query.Where(x => x.Rating == request.Rating);

        if (request.IsHidden.HasValue)
            query = query.Where(x => x.IsHidden == request.IsHidden);

        if (request.HasStaffReply == true)
            query = query.Where(x => x.StaffReply != null && x.StaffReply != string.Empty);

        if (request.HasStaffReply == false)
            query = query.Where(x => x.StaffReply == null || x.StaffReply == string.Empty);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Comment != null && x.Comment.ToLower().Contains(term))
                || (x.StaffReply != null && x.StaffReply.ToLower().Contains(term))
                || (x.Customer.Account != null && x.Customer.Account.Fullname != null
                    && x.Customer.Account.Fullname.ToLower().Contains(term))
                || (x.Customer.Account != null && x.Customer.Account.Username.ToLower().Contains(term))
                || x.DesignTemplate.Name.ToLower().Contains(term)
                || (x.OrderItem.ItemName != null && x.OrderItem.ItemName.ToLower().Contains(term)));
        }

        return await query.ToPaginatedListAsync(_mapper, request.PageNumber, request.PageSize, ct);
    }
}
