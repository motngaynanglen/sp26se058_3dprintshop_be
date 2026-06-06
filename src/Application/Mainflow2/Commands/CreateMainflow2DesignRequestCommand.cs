using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

public record CreateMainflow2DesignRequestCommand : IRequest<Guid>
{
    public string? Title { get; init; }
    public required string RequirementBrief { get; init; }
    public IReadOnlyList<string> InitialIdeaImageUrls { get; init; } = Array.Empty<string>();
}

public class CreateMainflow2DesignRequestCommandValidator : AbstractValidator<CreateMainflow2DesignRequestCommand>
{
    public CreateMainflow2DesignRequestCommandValidator()
    {
        RuleFor(x => x.RequirementBrief).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.Title).MaximumLength(500);
        RuleFor(x => x.InitialIdeaImageUrls).Must(x => x.Count <= 20).WithMessage("Tối đa 20 ảnh.");
    }
}

public class CreateMainflow2DesignRequestCommandHandler : IRequestHandler<CreateMainflow2DesignRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public CreateMainflow2DesignRequestCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Guid> Handle(CreateMainflow2DesignRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Chỉ khách hàng mới tạo được yêu cầu này.");

        var urls = request.InitialIdeaImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToList();
        var json = urls.Count > 0 ? JsonSerializer.Serialize(urls) : null;

        var dw = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = request.Title,
            SourceType = SourceTypes.CustomQuoteMainflow2,
            CustomerId = customer.Id,
            Status = Mainflow2DesignWorkStatuses.Submitted,
            RequirementBrief = request.RequirementBrief.Trim(),
            InitialIdeaImageUrlsJson = json,
            BaseImageUrl = urls.FirstOrDefault(),
            QuoteRevision = 0,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "customer",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "customer"
        };

        _context.DesignWorks.Add(dw);

        var briefLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = request.RequirementBrief.Trim(),
            Metadata = json,
            LogType = Mainflow2DesignLogTypes.CustomerMessage,
            IsAI = false,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "customer",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "customer"
        };
        _context.DesignLogs.Add(briefLog);

        await _context.SaveChangesAsync(cancellationToken);

        await _realtime.NotifyAsync(dw.Id, "created", new { dw.Id, dw.Status }, cancellationToken);

        return dw.Id;
    }
}
