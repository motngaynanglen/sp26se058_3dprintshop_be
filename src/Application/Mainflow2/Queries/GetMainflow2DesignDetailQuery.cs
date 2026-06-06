using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2.Models;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Queries;

public record GetMainflow2DesignDetailQuery : IRequest<Mainflow2DesignDetailDto>
{
    public Guid DesignWorkId { get; init; }
}

public class GetMainflow2DesignDetailQueryHandler : IRequestHandler<GetMainflow2DesignDetailQuery, Mainflow2DesignDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2AccessibleFileUrlService _fileUrls;

    public GetMainflow2DesignDetailQueryHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2AccessibleFileUrlService fileUrls)
    {
        _context = context;
        _user = user;
        _fileUrls = fileUrls;
    }

    public async Task<Mainflow2DesignDetailDto> Handle(GetMainflow2DesignDetailQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();

        var dw = await _context.DesignWorks
            .AsNoTracking()
            .Include(d => d.Customer).ThenInclude(c => c.Account)
            .Include(d => d.MainAssignedStaff).ThenInclude(s => s!.Account)
            .FirstOrDefaultAsync(d => d.Id == request.DesignWorkId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");

        if (!Mainflow2DesignAccess.IsMainflow2(dw))
            throw new InvalidOperationException("Yêu cầu không thuộc luồng custom có báo giá.");

        var canCustomer = await Mainflow2DesignAccess.CanCustomerViewAsync(_context, accountId, dw, cancellationToken);
        var canStaff = await Mainflow2DesignAccess.CanStaffViewAsync(_context, accountId, _user.Role, dw, cancellationToken);
        if (!canCustomer && !canStaff)
            throw new UnauthorizedAccessException("Không có quyền xem yêu cầu này.");

        var logs = await _context.DesignLogs
            .AsNoTracking()
            .Include(l => l.Account).ThenInclude(a => a!.Customer)
            .Include(l => l.Account).ThenInclude(a => a!.Staff)
            .Include(l => l.Account).ThenInclude(a => a!.Manager)
            .Where(l => l.DesignWorkId == dw.Id)
            .OrderBy(l => l.Created)
            .ToListAsync(cancellationToken);

        var versions = await _context.DesignVersionHistorys
            .AsNoTracking()
            .Where(v => v.DesignWorkId == dw.Id)
            .OrderBy(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        var initialUrls = ParseUrls(dw.InitialIdeaImageUrlsJson);

        var linkedOrderRow = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.DesignWorkId == dw.Id && oi.Order.OrderStatus != OrderStatuses.Cancelled)
            .OrderByDescending(oi => oi.Created)
            .Select(oi => new
            {
                oi.OrderId,
                oi.Order.Code,
                oi.Order.OrderStatus,
                PaymentStatus = oi.Order.Invoice != null ? oi.Order.Invoice.PaymentStatus : null,
            })
            .FirstOrDefaultAsync(cancellationToken);

        string? linkedShipmentStatus = null;
        string? linkedTrackingNumber = null;
        if (linkedOrderRow != null)
        {
            var shipment = await _context.Shipments
                .AsNoTracking()
                .Where(s => s.OrderId == linkedOrderRow.OrderId)
                .OrderByDescending(s => s.Created)
                .Select(s => new { s.ShipmentStatus, s.TrackingNumber })
                .FirstOrDefaultAsync(cancellationToken);
            linkedShipmentStatus = shipment?.ShipmentStatus;
            linkedTrackingNumber = shipment?.TrackingNumber;
        }

        var orderId = linkedOrderRow?.OrderId;
        LinkedOrderTimelineSnapshot? linkedSnapshot = linkedOrderRow is null
            ? null
            : new LinkedOrderTimelineSnapshot
            {
                OrderId = linkedOrderRow.OrderId,
                OrderCode = linkedOrderRow.Code,
                OrderStatus = linkedOrderRow.OrderStatus,
                PaymentStatus = linkedOrderRow.PaymentStatus,
                ShipmentStatus = linkedShipmentStatus,
            };

        // Với CUSTOM_FILE_PRINT_MF2: file gốc của khách lưu trong BaseImageUrl
        // và một số thông số bổ sung trong RequirementBrief (chuỗi mô tả) hoặc trong metadata của log đầu tiên (JSON).
        string? customerFileUrl = null;
        Guid? requestedMaterialId = null;
        string? requestedMaterialName = null;
        string? technicalRequirements = null;
        if (dw.SourceType == SourceTypes.AiGenerated)
        {
            customerFileUrl = versions.FirstOrDefault()?.FileUrl;
            if (string.IsNullOrWhiteSpace(technicalRequirements) && !string.IsNullOrWhiteSpace(dw.RequirementBrief))
                technicalRequirements = dw.RequirementBrief;
        }
        else if (dw.SourceType == SourceTypes.CustomFilePrintMainflow2)
        {
            customerFileUrl = dw.BaseImageUrl;
            var firstLog = logs.FirstOrDefault();
            if (firstLog?.Metadata is { } meta && !string.IsNullOrWhiteSpace(meta))
            {
                try
                {
                    using var doc = JsonDocument.Parse(meta);
                    if (doc.RootElement.TryGetProperty("materialId", out var mid) && mid.ValueKind == JsonValueKind.String && Guid.TryParse(mid.GetString(), out var g))
                        requestedMaterialId = g;
                    if (doc.RootElement.TryGetProperty("technicalRequirements", out var tr) && tr.ValueKind == JsonValueKind.String)
                        technicalRequirements = tr.GetString();
                }
                catch { /* metadata không phải JSON hợp lệ — bỏ qua */ }
            }
            if (requestedMaterialId is { } mid2)
            {
                requestedMaterialName = await _context.Materials
                    .AsNoTracking()
                    .Where(m => m.Id == mid2)
                    .Select(m => m.Name)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        if (!string.IsNullOrWhiteSpace(customerFileUrl))
            customerFileUrl = _fileUrls.Resolve(customerFileUrl);

        var initialUrlsResolved = initialUrls.Select(u => _fileUrls.Resolve(u) ?? u).ToList();

        var messages = logs.Select(l => new Mainflow2MessageDto
        {
            Id = l.Id,
            AccountId = l.AccountId,
            LogType = l.LogType,
            Content = l.Content,
            MetadataJson = l.LogType == Mainflow2DesignLogTypes.StaffQuote
                ? _fileUrls.RewriteStaffQuoteMetadataForDisplay(l.Metadata, dw.SourceType, customerFileUrl)
                : l.LogType == Mainflow2DesignLogTypes.DesignReady
                    ? ResolveDesignReadyMetadata(l.Metadata)
                    : l.Metadata,
            Created = l.Created,
            AuthorName = l.Account?.Fullname,
            AuthorRole = ResolveAuthorRole(l)
        }).ToList();

        var versionsDto = versions.Select(v => new Mainflow2VersionDto
        {
            Id = v.Id,
            VersionNumber = v.VersionNumber,
            Title = v.Tilte,
            FileUrl = _fileUrls.Resolve(v.FileUrl) ?? v.FileUrl,
            IsPreviewable = v.IsPreviewable,
            IsApproved = v.IsApproved,
            IsPrintable = v.IsPrintable,
            Created = v.Created
        }).ToList();

        var latestQuotePreviewUrl = dw.SourceType == SourceTypes.AiGenerated
            ? customerFileUrl
            : versionsDto
                .Where(v => v.IsPreviewable && v.Title?.Contains("báo giá", StringComparison.OrdinalIgnoreCase) == true)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => v.FileUrl)
                .FirstOrDefault();

        var latestQuoteLog = logs
            .LastOrDefault(l => l.LogType == Mainflow2DesignLogTypes.StaffQuote);
        var latestQuoteDesignFee = Mainflow2QuoteFeeHelper.TryParseDesignFeeFromQuoteMetadata(latestQuoteLog?.Metadata);
        var latestQuoteMaterialSubtotal = Mainflow2QuoteFeeHelper.TryParseMaterialSubtotalFromQuoteMetadata(latestQuoteLog?.Metadata);

        var designReadyForBalance = await Mainflow2DesignFlowHelper.IsDesignReadyForBalanceAsync(
            _context, dw.Id, cancellationToken);

        decimal? remainingBalance = null;
        if (linkedOrderRow != null)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Transactions)
                .FirstOrDefaultAsync(i => i.OrderId == linkedOrderRow.OrderId, cancellationToken);
            if (invoice != null && OrderPaymentHelper.IsInvoicePartiallyPaid(invoice))
                remainingBalance = OrderPaymentHelper.GetRemainingBalance(invoice);
        }

        return new Mainflow2DesignDetailDto
        {
            Id = dw.Id,
            Title = dw.Name,
            Status = dw.Status,
            SourceType = dw.SourceType,
            RequirementBrief = dw.RequirementBrief,
            InitialIdeaImageUrls = initialUrlsResolved,
            CustomerFileUrl = customerFileUrl,
            LatestQuotePreviewUrl = latestQuotePreviewUrl,
            RequestedMaterialId = requestedMaterialId,
            RequestedMaterialName = requestedMaterialName,
            TechnicalRequirements = technicalRequirements,
            CustomerId = dw.CustomerId,
            CustomerName = dw.Customer?.Account?.Fullname,
            MainAssignedStaffId = dw.MainAssignedStaffId,
            MainAssignedStaffName = dw.MainAssignedStaff?.Account?.Fullname,
            LatestQuotedPrice = dw.LatestQuotedPrice,
            LatestQuoteDesignFee = latestQuoteDesignFee,
            LatestQuoteMaterialSubtotal = latestQuoteMaterialSubtotal,
            QuoteRevision = dw.QuoteRevision,
            StaffAssignedAt = dw.StaffAssignedAt,
            LastQuotedAt = dw.LastQuotedAt,
            CustomerApprovedAt = dw.CustomerApprovedAt,
            Created = dw.Created,
            LastModified = dw.LastModified,
            OrderId = orderId,
            LinkedOrderCode = linkedOrderRow?.Code,
            LinkedOrderStatus = linkedOrderRow?.OrderStatus,
            LinkedPaymentStatus = linkedOrderRow?.PaymentStatus,
            LinkedShipmentStatus = linkedShipmentStatus,
            LinkedTrackingNumber = linkedTrackingNumber,
            DesignReadyForBalance = designReadyForBalance,
            RemainingBalance = remainingBalance,
            Messages = messages,
            Versions = versionsDto,
            Timeline = Mainflow2TimelineBuilder.Build(dw, linkedSnapshot, designReadyForBalance)
        };
    }

    private static IReadOnlyList<string> ParseUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string ResolveAuthorRole(DesignLog log)
    {
        if (log.Account == null) return "SYSTEM";
        if (log.Account.Manager != null) return Roles.MANAGER;
        if (log.Account.Staff != null) return Roles.STAFF;
        if (log.Account.Customer != null) return Roles.CUSTOMER;
        return Roles.GUEST;
    }

    private string? ResolveDesignReadyMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return metadataJson;

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return metadataJson;

            var deliverable = root.TryGetProperty("deliverableFileUrl", out var urlEl) && urlEl.ValueKind == JsonValueKind.String
                ? _fileUrls.Resolve(urlEl.GetString()!) ?? urlEl.GetString()
                : null;

            string[]? deliverables = null;
            if (root.TryGetProperty("deliverableFileUrls", out var urlsEl) && urlsEl.ValueKind == JsonValueKind.Array)
            {
                deliverables = urlsEl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => _fileUrls.Resolve(e.GetString()!) ?? e.GetString()!)
                    .ToArray();
            }

            return JsonSerializer.Serialize(new
            {
                deliverableFileUrl = deliverable,
                deliverableFileUrls = deliverables ?? (deliverable != null ? new[] { deliverable } : Array.Empty<string>())
            });
        }
        catch
        {
            return metadataJson;
        }
    }
}
