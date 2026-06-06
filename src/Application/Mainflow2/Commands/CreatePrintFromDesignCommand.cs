using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

/// <summary>TH2: In từ thiết kế đã hoàn thành và thanh toán trên hệ thống.</summary>
public record CreatePrintFromDesignCommand : IRequest<Guid>
{
    public Guid SourceDesignWorkId { get; init; }
    public string? PrintPriority { get; init; }
    public int Quantity { get; init; } = 1;
    public string? PrintSize { get; init; }
    public string? Note { get; init; }
}

public class CreatePrintFromDesignCommandValidator : AbstractValidator<CreatePrintFromDesignCommand>
{
    public CreatePrintFromDesignCommandValidator()
    {
        RuleFor(x => x.SourceDesignWorkId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PrintPriority).MaximumLength(100);
        RuleFor(x => x.PrintSize).MaximumLength(200);
        RuleFor(x => x.Note).MaximumLength(4000);
    }
}

public class CreatePrintFromDesignCommandHandler : IRequestHandler<CreatePrintFromDesignCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public CreatePrintFromDesignCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Guid> Handle(CreatePrintFromDesignCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Chỉ khách hàng mới tạo được yêu cầu in.");

        var source = await _context.DesignWorks
            .FirstOrDefaultAsync(d => d.Id == request.SourceDesignWorkId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy thiết kế nguồn.");

        if (source.CustomerId != customer.Id)
            throw new UnauthorizedAccessException("Thiết kế không thuộc về bạn.");

        if (source.SourceType != SourceTypes.CustomQuoteMainflow2)
            throw new InvalidOperationException("Chỉ in lại từ yêu cầu thiết kế đã hoàn thành.");

        if (source.Status != Mainflow2DesignWorkStatuses.Approved)
            throw new InvalidOperationException("Thiết kế chưa được duyệt hoàn tất.");

        if (!await Mainflow2PrintFlowHelper.HasPaidOrderAsync(_context, source.Id, cancellationToken))
            throw new InvalidOperationException("Cần thanh toán đầy đủ phí thiết kế trước khi in.");

        var printableVersions = await Mainflow2PrintFlowHelper.LoadPrintableVersionsAsync(_context, source.Id, cancellationToken);
        if (printableVersions.Count == 0)
        {
            var allVersions = await _context.DesignVersionHistorys
                .AsNoTracking()
                .Where(v => v.DesignWorkId == source.Id)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync(cancellationToken);
            printableVersions = allVersions
                .Where(v => Mainflow2PrintFlowHelper.IsPrintableFile(v.FileUrl))
                .ToList();
        }

        if (printableVersions.Count == 0)
            throw new InvalidOperationException("Thiết kế chưa có file in phù hợp (STL/OBJ/GLB).");

        var sourceLogs = await _context.DesignLogs
            .AsNoTracking()
            .Where(l => l.DesignWorkId == source.Id)
            .ToListAsync(cancellationToken);

        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? "customer";
        var brief = Mainflow2PrintFlowHelper.BuildPrintBriefNote(
            request.PrintPriority,
            request.Quantity,
            request.PrintSize,
            request.Note);

        var dw = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = $"In: {source.Name ?? "Thiết kế đã có"}",
            SourceType = SourceTypes.PrintFromDesignMainflow2,
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
            throw new InvalidOperationException("Không xác định được giá in — liên hệ shop.");

        var metadata = new
        {
            sourceDesignWorkId = source.Id,
            printPriority = request.PrintPriority,
            quantity = request.Quantity,
            printSize = request.PrintSize,
            quotedPrice = dw.LatestQuotedPrice,
            directPrint = true
        };
        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = string.IsNullOrWhiteSpace(brief)
                ? $"In từ thiết kế «{source.Name}» — {dw.LatestQuotedPrice:N0} VND/đơn vị."
                : brief,
            Metadata = JsonSerializer.Serialize(metadata),
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
        await _realtime.NotifyAsync(dw.Id, "approved", new { dw.Id, dw.Status, directPrint = true }, cancellationToken);

        return dw.Id;
    }
}
