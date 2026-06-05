using System.ComponentModel;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;

/// <summary>
/// Lấy danh sách feedback theo DesignVariantId (thông qua OrderItem.DesignVariantId).
/// Dùng cho trang chi tiết sản phẩm (variant detail) trên FE.
/// </summary>
public class GetFeedbacksByVariantQuery : PaginationRequest, IRequest<PaginatedList<FeedbackDTO>>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid VariantId { get; set; }

    [DefaultValue(null)]
    public int? Rating { get; init; }
}

public class GetFeedbacksByVariantQueryValidator : AbstractValidator<GetFeedbacksByVariantQuery>
{
    public GetFeedbacksByVariantQueryValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty().WithMessage("Mã biến thể là bắt buộc.");
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
    }
}

public class GetFeedbacksByVariantQueryHandler : IRequestHandler<GetFeedbacksByVariantQuery, PaginatedList<FeedbackDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetFeedbacksByVariantQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<FeedbackDTO>> Handle(GetFeedbacksByVariantQuery request, CancellationToken ct)
    {
        var query = _context.Feedbacks
            .AsNoTracking()
            .Where(f => f.OrderItem.DesignVariantId == request.VariantId && !f.IsHidden);

        if (request.Rating.HasValue)
        {
            query = query.Where(f => f.Rating == request.Rating);
        }

        return await query
            .OrderByDescending(f => f.Created)
            .ProjectTo<FeedbackDTO>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
