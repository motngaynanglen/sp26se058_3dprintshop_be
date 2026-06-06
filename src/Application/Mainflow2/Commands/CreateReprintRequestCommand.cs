using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

/// <summary>TH3: In lại đơn custom đã có báo giá — sao chép báo giá, chuyển thẳng sang APPROVED.</summary>
public record CreateReprintRequestCommand : IRequest<Guid>
{
    public Guid SourceDesignWorkId { get; init; }
    public int Quantity { get; init; } = 1;
    public string? Note { get; init; }
}

public class CreateReprintRequestCommandValidator : AbstractValidator<CreateReprintRequestCommand>
{
    public CreateReprintRequestCommandValidator()
    {
        RuleFor(x => x.SourceDesignWorkId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(4000);
    }
}

public class CreateReprintRequestCommandHandler : IRequestHandler<CreateReprintRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public CreateReprintRequestCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Guid> Handle(CreateReprintRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Chỉ khách hàng mới in lại.");

        var source = await _context.DesignWorks
            .FirstOrDefaultAsync(d => d.Id == request.SourceDesignWorkId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn in nguồn.");

        if (source.CustomerId != customer.Id)
            throw new UnauthorizedAccessException("Đơn in không thuộc về bạn.");

        if (!SourceTypes.IsCustomPrintFlow(source.SourceType) && source.SourceType != SourceTypes.CustomQuoteMainflow2)
            throw new InvalidOperationException("Chỉ in lại từ đơn custom đã có báo giá.");

        if (source.LatestQuotedPrice is null or <= 0)
            throw new InvalidOperationException("Đơn nguồn chưa có báo giá để in lại.");

        var hasQuoteOrOrder = source.Status is Mainflow2DesignWorkStatuses.Approved or Mainflow2DesignWorkStatuses.Quoted
                              or Mainflow2DesignWorkStatuses.Negotiating
                              || await Mainflow2PrintFlowHelper.HasPaidOrderAsync(_context, source.Id, cancellationToken);

        if (!hasQuoteOrOrder)
            throw new InvalidOperationException("Đơn nguồn chưa đủ điều kiện để in lại.");

        var printableVersions = await Mainflow2PrintFlowHelper.LoadPrintableVersionsAsync(_context, source.Id, cancellationToken);
        if (printableVersions.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(source.BaseImageUrl)
                && Mainflow2PrintFlowHelper.IsPrintableFile(source.BaseImageUrl))
            {
                printableVersions =
                [
                    new DesignVersionHistory
                    {
                        FileUrl = source.BaseImageUrl,
                        VersionNumber = 1,
                        IsPrintable = true,
                        IsPreviewable = true,
                        Tilte = "File in"
                    }
                ];
            }
        }

        if (printableVersions.Count == 0)
            throw new InvalidOperationException("Không tìm thấy file in để sao chép.");

        var sourceLogs = await _context.DesignLogs
            .AsNoTracking()
            .Where(l => l.DesignWorkId == source.Id)
            .ToListAsync(cancellationToken);

        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? "customer";
        var brief = Mainflow2PrintFlowHelper.BuildPrintBriefNote(null, request.Quantity, null, request.Note);

        var dw = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = $"In lại: {source.Name ?? "Đơn custom"}",
            SourceType = SourceTypes.ReprintMainflow2,
            SourceDesignWorkId = source.Id,
            CustomerId = customer.Id,
            Status = Mainflow2DesignWorkStatuses.Submitted,
            RequirementBrief = string.IsNullOrWhiteSpace(brief) ? source.RequirementBrief : brief,
            BaseImageUrl = printableVersions[0].FileUrl,
            QuoteRevision = source.QuoteRevision,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        };
        _context.DesignWorks.Add(dw);

        Mainflow2PrintFlowHelper.CopyVersionHistories(_context, dw, printableVersions, accountId, username, now);

        var quoteLog = Mainflow2PrintFlowHelper.FindLatestQuoteLog(sourceLogs);
        Mainflow2PrintFlowHelper.ApplyCopiedQuote(dw, quoteLog, now, username, autoApprove: true);

        var unitPrice = await Mainflow2PrintFlowHelper.ResolvePrintUnitPriceAsync(
            _context, source, quoteLog, cancellationToken);
        Mainflow2PrintFlowHelper.FinalizeDirectPrintDesignWork(dw, unitPrice, source, now, username);

        if (dw.LatestQuotedPrice is null or <= 0)
            throw new InvalidOperationException("Không xác định được giá in để in lại.");

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = string.IsNullOrWhiteSpace(brief)
                ? $"In lại đơn — báo giá {dw.LatestQuotedPrice:N0} VND/đơn vị."
                : brief,
            Metadata = JsonSerializer.Serialize(new
            {
                sourceDesignWorkId = source.Id,
                quantity = request.Quantity,
                quotedPrice = dw.LatestQuotedPrice,
                reprint = true
            }),
            LogType = Mainflow2DesignLogTypes.CustomerMessage,
            IsAI = false,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        });

        if (quoteLog != null)
        {
            _context.DesignLogs.Add(new DesignLog
            {
                Id = Guid.NewGuid(),
                DesignWorkId = dw.Id,
                AccountId = quoteLog.AccountId,
                Content = quoteLog.Content,
                Metadata = quoteLog.Metadata,
                LogType = Mainflow2DesignLogTypes.StaffQuote,
                IsAI = false,
                Created = now,
                CreatedBy = username,
                LastModified = now,
                LastModifiedBy = username
            });
        }

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = Mainflow2DesignWorkStatuses.Approved,
            LogType = Mainflow2DesignLogTypes.StatusChange,
            IsAI = false,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyAsync(dw.Id, "approved", new { dw.Id, dw.Status, reprint = true }, cancellationToken);

        return dw.Id;
    }
}
