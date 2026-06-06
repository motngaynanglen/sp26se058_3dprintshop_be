using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Application.Mainflow2.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Queries;

/// <summary>TH2: Danh sách thiết kế đã thanh toán đủ — khách có thể chọn In ngay.</summary>
public record ListPrintableDesignsQuery : IRequest<IReadOnlyList<PrintableDesignListItemDto>>;

public class PrintableDesignListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PreviewFileUrl { get; set; }
    public DateTimeOffset Created { get; set; }
    public decimal? DesignFeePaid { get; set; }
}

public class ListPrintableDesignsQueryHandler : IRequestHandler<ListPrintableDesignsQuery, IReadOnlyList<PrintableDesignListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2AccessibleFileUrlService _fileUrls;

    public ListPrintableDesignsQueryHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2AccessibleFileUrlService fileUrls)
    {
        _context = context;
        _user = user;
        _fileUrls = fileUrls;
    }

    public async Task<IReadOnlyList<PrintableDesignListItemDto>> Handle(
        ListPrintableDesignsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Chỉ khách hàng xem được danh sách này.");

        var candidates = await _context.DesignWorks
            .AsNoTracking()
            .Where(d => d.CustomerId == customer.Id
                        && d.SourceType == SourceTypes.CustomQuoteMainflow2
                        && d.Status == Mainflow2DesignWorkStatuses.Approved)
            .OrderByDescending(d => d.Created)
            .ToListAsync(cancellationToken);

        var result = new List<PrintableDesignListItemDto>();
        foreach (var dw in candidates)
        {
            if (!await Mainflow2PrintFlowHelper.HasPaidOrderAsync(_context, dw.Id, cancellationToken))
                continue;

            var preview = await Mainflow2PrintFlowHelper.ResolveListPreviewFileUrlAsync(
                _context, dw, cancellationToken);
            if (!string.IsNullOrWhiteSpace(preview))
                preview = _fileUrls.Resolve(preview) ?? preview;

            result.Add(new PrintableDesignListItemDto
            {
                Id = dw.Id,
                Title = dw.Name ?? "Thiết kế",
                PreviewFileUrl = preview,
                Created = dw.Created,
                DesignFeePaid = dw.LatestQuotedPrice
            });
        }

        return result;
    }
}
