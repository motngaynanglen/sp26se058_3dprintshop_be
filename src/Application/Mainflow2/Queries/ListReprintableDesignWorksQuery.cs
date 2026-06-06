using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Queries;

/// <summary>TH3: Đơn custom đã có báo giá — khách có thể In lại.</summary>
public record ListReprintableDesignWorksQuery : IRequest<IReadOnlyList<ReprintableDesignWorkListItemDto>>;

public class ReprintableDesignWorkListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? LatestQuotedPrice { get; set; }
    public string? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PreviewFileUrl { get; set; }
    public DateTimeOffset Created { get; set; }
}

public class ListReprintableDesignWorksQueryHandler
    : IRequestHandler<ListReprintableDesignWorksQuery, IReadOnlyList<ReprintableDesignWorkListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2AccessibleFileUrlService _fileUrls;

    public ListReprintableDesignWorksQueryHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2AccessibleFileUrlService fileUrls)
    {
        _context = context;
        _user = user;
        _fileUrls = fileUrls;
    }

    public async Task<IReadOnlyList<ReprintableDesignWorkListItemDto>> Handle(
        ListReprintableDesignWorksQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Chỉ khách hàng xem được danh sách này.");

        var printSourceTypes = new[]
        {
            SourceTypes.CustomFilePrintMainflow2,
            SourceTypes.AiGenerated,
            SourceTypes.PrintFromDesignMainflow2,
        };

        var items = await _context.DesignWorks
            .AsNoTracking()
            .Where(d => d.CustomerId == customer.Id
                        && printSourceTypes.Contains(d.SourceType)
                        && d.LatestQuotedPrice != null
                        && d.LatestQuotedPrice > 0
                        && d.Status != Mainflow2DesignWorkStatuses.Cancelled)
            .OrderByDescending(d => d.LastQuotedAt ?? d.Created)
            .ToListAsync(cancellationToken);

        var result = new List<ReprintableDesignWorkListItemDto>();
        foreach (var dw in items)
        {
            var linked = await _context.OrderItems
                .AsNoTracking()
                .Where(oi => oi.DesignWorkId == dw.Id && oi.Order.OrderStatus != OrderStatuses.Cancelled)
                .OrderByDescending(oi => oi.Created)
                .Select(oi => new
                {
                    oi.Order.OrderStatus,
                    PaymentStatus = oi.Order.Invoice != null ? oi.Order.Invoice.PaymentStatus : null
                })
                .FirstOrDefaultAsync(cancellationToken);

            var eligible = dw.Status == Mainflow2DesignWorkStatuses.Approved
                           || linked != null
                           || dw.LastQuotedAt != null;

            if (!eligible) continue;

            var hasPendingReprint = await _context.DesignWorks
                .AnyAsync(d => d.SourceDesignWorkId == dw.Id
                               && d.SourceType == SourceTypes.ReprintMainflow2
                               && d.Status != Mainflow2DesignWorkStatuses.Cancelled
                               && !_context.OrderItems.Any(oi =>
                                   oi.DesignWorkId == d.Id
                                   && oi.Order.OrderStatus != OrderStatuses.Cancelled
                                   && oi.Order.Invoice != null
                                   && oi.Order.Invoice.PaymentStatus == InvoiceStatuses.Paid),
                    cancellationToken);

            if (hasPendingReprint) continue;

            var preview = await Mainflow2PrintFlowHelper.ResolveListPreviewFileUrlAsync(
                _context, dw, cancellationToken);
            if (!string.IsNullOrWhiteSpace(preview))
                preview = _fileUrls.Resolve(preview) ?? preview;

            result.Add(new ReprintableDesignWorkListItemDto
            {
                Id = dw.Id,
                Title = dw.Name ?? "Đơn in custom",
                SourceType = dw.SourceType,
                Status = dw.Status,
                LatestQuotedPrice = dw.LatestQuotedPrice,
                OrderStatus = linked?.OrderStatus,
                PaymentStatus = linked?.PaymentStatus,
                PreviewFileUrl = preview,
                Created = dw.Created
            });
        }

        return result;
    }
}
