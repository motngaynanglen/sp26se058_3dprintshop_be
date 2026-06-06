using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

/// <summary>
/// Luồng 3: Khách prompt + AI sinh GLB → gửi yêu cầu báo giá (giống flow 2, KTV báo giá từ file GLB).
/// </summary>
public record CreateAiGeneratedPrintRequestCommand : IRequest<Guid>
{
    public string? Title { get; init; }

    /// <summary>URL file GLB do AI sinh ra (sau POST /api/model-generate).</summary>
    public required string ModelFileUrl { get; init; }

    /// <summary>URL ảnh khách dùng làm prompt (nếu có).</summary>
    public string? SourceImageUrl { get; init; }

    /// <summary>Prompt / mô tả khách gửi cho AI.</summary>
    public string? Prompt { get; init; }

    /// <summary>Giá in (VND) — nếu có thì khách có thể checkout ngay sau khi tạo mẫu.</summary>
    public decimal? QuotedPrice { get; init; }
}

public class CreateAiGeneratedPrintRequestCommandValidator : AbstractValidator<CreateAiGeneratedPrintRequestCommand>
{
    public CreateAiGeneratedPrintRequestCommandValidator()
    {
        RuleFor(x => x.ModelFileUrl).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Title).MaximumLength(500);
        RuleFor(x => x.SourceImageUrl).MaximumLength(1000);
        RuleFor(x => x.Prompt).MaximumLength(4000);
        RuleFor(x => x.ModelFileUrl).Must(BeGlb)
            .WithMessage("ModelFileUrl phải là file .glb.");
    }

    private static bool BeGlb(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return url.Trim().ToLowerInvariant().Contains(".glb");
    }
}

public class CreateAiGeneratedPrintRequestCommandHandler
    : IRequestHandler<CreateAiGeneratedPrintRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public CreateAiGeneratedPrintRequestCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Guid> Handle(CreateAiGeneratedPrintRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Chỉ khách hàng mới tạo được yêu cầu in từ mô hình AI.");

        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? "customer";
        var glbUrl = request.ModelFileUrl.Trim();
        var prompt = request.Prompt?.Trim();

        var ideaUrls = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.SourceImageUrl))
            ideaUrls.Add(request.SourceImageUrl.Trim());

        var dw = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(request.Title)
                ? $"Mô hình AI ({now:dd/MM/yyyy HH:mm})"
                : request.Title!.Trim(),
            SourceType = SourceTypes.AiGenerated,
            CustomerId = customer.Id,
            Status = request.QuotedPrice is > 0
                ? Mainflow2DesignWorkStatuses.Quoted
                : Mainflow2DesignWorkStatuses.Submitted,
            RequirementBrief = prompt,
            BaseImageUrl = request.SourceImageUrl?.Trim(),
            InitialIdeaImageUrlsJson = ideaUrls.Count > 0 ? JsonSerializer.Serialize(ideaUrls) : null,
            LatestQuotedPrice = request.QuotedPrice is > 0 ? request.QuotedPrice : null,
            LastQuotedAt = request.QuotedPrice is > 0 ? now : null,
            QuoteRevision = request.QuotedPrice is > 0 ? 1 : 0,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        };
        _context.DesignWorks.Add(dw);

        _context.DesignVersionHistorys.Add(new DesignVersionHistory
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            UploaderId = accountId,
            FileUrl = glbUrl,
            VersionNumber = 1,
            Tilte = "GLB từ AI",
            IsPreviewable = true,
            IsApproved = false,
            IsPrintable = true,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        });

        var metadata = new
        {
            sourceImageUrl = request.SourceImageUrl,
            prompt,
            modelFileUrl = glbUrl,
            generatedBy = "AI"
        };
        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = string.IsNullOrWhiteSpace(prompt)
                ? "Tôi đã tạo mô hình 3D bằng AI và cần báo giá in."
                : prompt,
            Metadata = JsonSerializer.Serialize(metadata),
            LogType = Mainflow2DesignLogTypes.CustomerMessage,
            IsAI = true,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        });

        await _context.SaveChangesAsync(cancellationToken);

        await _realtime.NotifyAsync(dw.Id, "created", new
        {
            id = dw.Id,
            status = dw.Status,
            sourceType = dw.SourceType,
            modelFileUrl = glbUrl
        }, cancellationToken);

        return dw.Id;
    }
}
