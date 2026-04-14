using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
public class GetFeedbacksByTemplateWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<FeedbackDTO>>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid TemplateId { get; init; }

    [DefaultValue(5)]
    public int? Rating { get; init; }
}
public class GetFeedbacksByTemplateWithPaginationQueryValidator : AbstractValidator<GetFeedbacksByTemplateWithPaginationQuery>
{
    public GetFeedbacksByTemplateWithPaginationQueryValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty().WithMessage("TemplateId là bắt buộc.");
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
    }
}
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
            .AsNoTracking()
            .Where(f => f.DesignTemplateId == request.TemplateId && !f.IsHidden);

        if (request.Rating.HasValue)
        {
            query = query.Where(f => f.Rating == request.Rating);
        }

        return await query
            .OrderByDescending(f => f.Created) // Luôn hiện đánh giá mới nhất lên đầu
            .ProjectTo<FeedbackDTO>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}

