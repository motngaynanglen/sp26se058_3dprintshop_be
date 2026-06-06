using System.ComponentModel;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Mainflow2.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Queries;

public class ListMainflow2DesignRequestsQuery : PaginationRequest, IRequest<PaginatedList<Mainflow2DesignListItemDto>>
{
    public string? Status { get; init; }

    /// <summary>Lọc theo nhóm: <c>design</c> (mô tả/ảnh) hoặc <c>print</c> (file STL/AI).</summary>
    public string? Category { get; init; }

    [DefaultValue("created")]
    public string? SortBy { get; init; }

    [DefaultValue(true)]
    public bool SortDescending { get; init; } = true;
}

public class ListMainflow2DesignRequestsQueryHandler
    : IRequestHandler<ListMainflow2DesignRequestsQuery, PaginatedList<Mainflow2DesignListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public ListMainflow2DesignRequestsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<PaginatedList<Mainflow2DesignListItemDto>> Handle(
        ListMainflow2DesignRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();

        var q = _context.DesignWorks
            .AsNoTracking()
            .Where(d => d.SourceType == SourceTypes.CustomQuoteMainflow2
                       || d.SourceType == SourceTypes.CustomFilePrintMainflow2
                       || d.SourceType == SourceTypes.AiGenerated
                       || d.SourceType == SourceTypes.PrintFromDesignMainflow2
                       || d.SourceType == SourceTypes.ReprintMainflow2);

        if (!string.IsNullOrWhiteSpace(request.Status))
            q = q.Where(d => d.Status == request.Status);

        q = (request.Category ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "design" => q.Where(d => d.SourceType == SourceTypes.CustomQuoteMainflow2),
            "print" => q.Where(d => d.SourceType == SourceTypes.CustomFilePrintMainflow2
                                    || d.SourceType == SourceTypes.AiGenerated
                                    || d.SourceType == SourceTypes.PrintFromDesignMainflow2
                                    || d.SourceType == SourceTypes.ReprintMainflow2),
            _ => q
        };

        if (_user.Role == Roles.CUSTOMER)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken)
                ?? throw new UnauthorizedAccessException("Chỉ khách hàng mới xem được danh sách của mình.");
            q = q.Where(d => d.CustomerId == customer.Id
                             && d.SourceType != SourceTypes.ReprintMainflow2
                             && d.SourceType != SourceTypes.PrintFromDesignMainflow2);
        }
        else if (_user.Role == Roles.STAFF)
        {
            var staff = await _context.Staffs
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.AccountId == accountId, cancellationToken)
                ?? throw new UnauthorizedAccessException("Chỉ nhân viên mới xem được danh sách yêu cầu.");
            q = q.Where(d => d.MainAssignedStaffId == null || d.MainAssignedStaffId == staff.Id);
        }
        else if (_user.Role is not Roles.MANAGER and not Roles.ADMIN)
        {
            throw new UnauthorizedAccessException("Không có quyền xem danh sách yêu cầu.");
        }

        var sorted = (request.SortBy ?? "created").Trim().ToLowerInvariant() switch
        {
            "price" => request.SortDescending
                ? q.OrderByDescending(d => d.LatestQuotedPrice ?? decimal.MinValue)
                : q.OrderBy(d => d.LatestQuotedPrice ?? decimal.MaxValue),
            "title" => request.SortDescending
                ? q.OrderByDescending(d => d.Name)
                : q.OrderBy(d => d.Name),
            _ => request.SortDescending
                ? q.OrderByDescending(d => d.Created)
                : q.OrderBy(d => d.Created),
        };

        var projected = sorted.Select(d => new Mainflow2DesignListItemDto
        {
            Id = d.Id,
            Title = d.Name,
            Status = d.Status,
            LatestQuotedPrice = d.LatestQuotedPrice,
            QuoteRevision = d.QuoteRevision,
            Created = d.Created,
            MainAssignedStaffId = d.MainAssignedStaffId,
            SourceType = d.SourceType,
            CustomerFileUrl = SourceTypes.IsCustomPrintFlow(d.SourceType)
                ? d.BaseImageUrl
                : null
        });

        return await projected.PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
