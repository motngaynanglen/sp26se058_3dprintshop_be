using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Models;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
public class GetFeedbacksByTemplateWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<FeedbackDTO>>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid TemplateId { get; init; }
    [DefaultValue(5)]
    public int? Rating { get; init; } // Filter theo số sao (1-5)
    public class GetFeedbacksByTemplateWithPaginationQueryHandler : IRequestHandler<GetFeedbacksByTemplateWithPaginationQuery, PaginatedList<FeedbackDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        public GetFeedbacksByTemplateWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<PaginatedList<FeedbackDTO>> Handle(GetFeedbacksByTemplateWithPaginationQuery request, CancellationToken ct)
        {
            var query = _context.Feedbacks
                .Where(f => f.DesignTemplateId == request.TemplateId && !f.IsHidden);
            if (request.Rating is >= 1 and <= 5)
                query = query.Where(f => f.Rating == request.Rating);

            return await query.ToPaginatedListAsync(_mapper, request.PageNumber, request.PageSize, ct);
        }
    }
}
