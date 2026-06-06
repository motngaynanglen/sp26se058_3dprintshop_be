using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Application.Mainflow2.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

public record StaffSubmitMainflow2QuoteCommand : IRequest<Unit>
{
    public Guid DesignWorkId { get; init; }

    /// <summary>Giá tổng — nếu có <see cref="Components"/> thì server tính lại từ vật liệu + tiền công.</summary>
    public decimal QuotedPrice { get; init; }

    public string Currency { get; init; } = "VND";
    public string? StaffNote { get; init; }

    /// <summary>Tiền công / gia công tổng (VND).</summary>
    public decimal LaborCost { get; init; }

    /// <summary>Thành phần sản phẩm + vật liệu + định lượng (gram).</summary>
    public IReadOnlyList<QuoteComponentInput>? Components { get; init; }

    /// <summary>URL file GLB tham khảo (tùy chọn — bảng thiết kế gửi sau cọc).</summary>
    public string? PreviewGlbUrl { get; init; }

    /// <summary>URL file thêm (STL/OBJ/GLB khác) đính kèm báo giá.</summary>
    public IReadOnlyList<string>? DesignFileUrls { get; init; }
}

public class StaffSubmitMainflow2QuoteCommandValidator : AbstractValidator<StaffSubmitMainflow2QuoteCommand>
{
    public StaffSubmitMainflow2QuoteCommandValidator()
    {
        RuleFor(x => x.QuotedPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LaborCost).GreaterThanOrEqualTo(0);

        When(x => x.Components is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Components!).ChildRules(c =>
            {
                c.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
                c.RuleFor(x => x.Quantity).GreaterThan(0);
                c.RuleFor(x => x.Materials).NotEmpty();
                c.RuleForEach(x => x.Materials).ChildRules(m =>
                {
                    m.RuleFor(x => x.MaterialId).NotEmpty();
                    m.RuleFor(x => x.Grams).GreaterThan(0);
                });
            });
        });

        RuleFor(x => x)
            .Must(x => x.QuotedPrice > 0 || x.Components is { Count: > 0 })
            .WithMessage("Cần nhập giá tổng hoặc bảng báo giá chi tiết (thành phần + vật liệu).");

        When(x => !string.IsNullOrWhiteSpace(x.PreviewGlbUrl), () =>
        {
            RuleFor(x => x.PreviewGlbUrl!)
                .Must(BeGlbUrl)
                .WithMessage("PreviewGlbUrl phải là file .glb.");
        });

        When(x => x.DesignFileUrls is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.DesignFileUrls!).NotEmpty();
        });
    }

    private static bool BeGlbUrl(string url) =>
        !string.IsNullOrWhiteSpace(url)
        && url.Trim().ToLowerInvariant().Contains(".glb");
}

public class StaffSubmitMainflow2QuoteCommandHandler : IRequestHandler<StaffSubmitMainflow2QuoteCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public StaffSubmitMainflow2QuoteCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Unit> Handle(StaffSubmitMainflow2QuoteCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var staff = await Mainflow2DesignAccess.EnsureStaffAsync(_context, accountId, cancellationToken);

        var dw = await _context.DesignWorks.FirstOrDefaultAsync(d => d.Id == request.DesignWorkId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");

        if (!Mainflow2DesignAccess.IsMainflow2(dw))
            throw new InvalidOperationException("Yêu cầu không thuộc Mainflow 2.");

        if (dw.MainAssignedStaffId != staff.Id)
            throw new UnauthorizedAccessException("Chỉ nhân viên được phân công mới gửi báo giá.");

        if (dw.Status is Mainflow2DesignWorkStatuses.Cancelled or Mainflow2DesignWorkStatuses.Approved)
            throw new InvalidOperationException("Không thể gửi báo giá ở trạng thái hiện tại.");

        Mainflow2QuoteBreakdownDto? breakdown = null;
        decimal quotedTotal = request.QuotedPrice;

        if (request.Components is { Count: > 0 })
        {
            var materialIds = request.Components
                .SelectMany(c => c.Materials.Select(m => m.MaterialId))
                .Distinct()
                .ToList();

            var materials = await _context.Materials
                .Include(m => m.PriceHistories)
                .Where(m => materialIds.Contains(m.Id))
                .ToListAsync(cancellationToken);

            var byId = materials.ToDictionary(m => m.Id);
            breakdown = Mainflow2QuotePricing.BuildBreakdown(
                request.Components,
                request.LaborCost,
                byId,
                request.Currency);
            quotedTotal = breakdown.Total;
        }
        else if (quotedTotal <= 0)
        {
            throw new InvalidOperationException("Giá báo phải lớn hơn 0 hoặc gửi bảng giá chi tiết.");
        }

        dw.QuoteRevision += 1;
        dw.LatestQuotedPrice = quotedTotal;
        dw.LastQuotedAt = CoreHelper.SystemTimeNow;
        dw.Status = Mainflow2DesignWorkStatuses.Quoted;
        dw.LastModified = CoreHelper.SystemTimeNow;
        dw.LastModifiedBy = _user.Username ?? "staff";

        var previewGlbUrl = ResolvePreviewGlbUrlAsync(request);

        var extra = request.DesignFileUrls ?? Array.Empty<string>();
        var allFileUrls = new List<string>();
        if (!string.IsNullOrWhiteSpace(previewGlbUrl))
            allFileUrls.Add(previewGlbUrl);
        foreach (var u in extra)
        {
            var t = u.Trim();
            if (!string.IsNullOrEmpty(t) && !allFileUrls.Contains(t, StringComparer.OrdinalIgnoreCase))
                allFileUrls.Add(t);
        }

        var metaObj = new
        {
            quotedPrice = quotedTotal,
            currency = request.Currency,
            laborCost = breakdown?.LaborCost ?? request.LaborCost,
            materialSubtotal = breakdown?.MaterialSubtotal,
            quoteBreakdown = breakdown,
            previewGlbUrl,
            designFileUrls = allFileUrls,
            revision = dw.QuoteRevision
        };
        var metaJson = JsonSerializer.Serialize(metaObj);

        var quoteLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = request.StaffNote?.Trim(),
            Metadata = metaJson,
            LogType = Mainflow2DesignLogTypes.StaffQuote,
            IsAI = false,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "staff",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "staff"
        };
        _context.DesignLogs.Add(quoteLog);

        var maxVer = await _context.DesignVersionHistorys
            .Where(v => v.DesignWorkId == dw.Id)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var fileIdx = 0;
        foreach (var url in allFileUrls)
        {
            maxVer++;
            fileIdx++;
            var vtitle = fileIdx == 1 && url.Contains(".glb", StringComparison.OrdinalIgnoreCase)
                ? $"GLB xem trước 3D (báo giá lần {dw.QuoteRevision})"
                : $"File đính kèm {fileIdx} (báo giá lần {dw.QuoteRevision})";
            _context.DesignVersionHistorys.Add(new DesignVersionHistory
            {
                Id = Guid.NewGuid(),
                DesignWorkId = dw.Id,
                DesignLogId = quoteLog.Id,
                UploaderId = accountId,
                FileUrl = url,
                VersionNumber = maxVer,
                Tilte = vtitle,
                IsPreviewable = true,
                IsApproved = false,
                IsPrintable = false,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "staff",
                LastModified = CoreHelper.SystemTimeNow,
                LastModifiedBy = _user.Username ?? "staff"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _realtime.NotifyAsync(dw.Id, "quote", new
        {
            dw.Id,
            dw.QuoteRevision,
            dw.LatestQuotedPrice,
            quoteBreakdown = breakdown,
            previewGlbUrl,
            designFileUrls = allFileUrls
        }, cancellationToken);

        return Unit.Value;
    }

    private static string? ResolvePreviewGlbUrlAsync(
        StaffSubmitMainflow2QuoteCommand request)
    {
        if (!string.IsNullOrWhiteSpace(request.PreviewGlbUrl))
            return request.PreviewGlbUrl.Trim();

        var glbFromAttachments = request.DesignFileUrls?
            .Select(u => u?.Trim())
            .FirstOrDefault(u => !string.IsNullOrEmpty(u) && u.Contains(".glb", StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(glbFromAttachments) ? null : glbFromAttachments;
    }
}
