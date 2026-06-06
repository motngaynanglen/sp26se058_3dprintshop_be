using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

/// <summary>
/// TH1: Khách upload file 3D (.STL/.OBJ/.GLB) → tạo DesignWork để KTV duyệt + báo giá + chat.
/// Khách không chọn vật liệu — KTV quyết định khi báo giá.
/// </summary>
public record CreateCustomFilePrintRequestCommand : IRequest<Guid>
{
    public string? Title { get; init; }

    /// <summary>URL file STL/OBJ/GLB khách đã upload thông qua <c>POST /api/files/upload</c>.</summary>
    public required string CustomerFileUrl { get; init; }

    /// <summary>Ưu tiên in: nhanh / chi tiết / rẻ / cân bằng.</summary>
    public string? PrintPriority { get; init; }

    /// <summary>Số lượng cần in.</summary>
    public int? Quantity { get; init; }

    /// <summary>Kích thước mong muốn (VD: 10x10x5 cm).</summary>
    public string? PrintSize { get; init; }

    /// <summary>Yêu cầu kỹ thuật bổ sung.</summary>
    public string? TechnicalRequirements { get; init; }

    /// <summary>Mô tả tổng quan / yêu cầu thêm.</summary>
    public string? Note { get; init; }

    /// <summary>Giữ tương thích API cũ — bị bỏ qua (khách không chọn vật liệu).</summary>
    [Obsolete("Khách phổ thông không chọn vật liệu — KTV quyết định khi báo giá.")]
    public Guid? MaterialId { get; init; }
}

public class CreateCustomFilePrintRequestCommandValidator : AbstractValidator<CreateCustomFilePrintRequestCommand>
{
    public CreateCustomFilePrintRequestCommandValidator()
    {
        RuleFor(x => x.CustomerFileUrl).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Title).MaximumLength(500);
        RuleFor(x => x.PrintPriority).MaximumLength(100);
        RuleFor(x => x.PrintSize).MaximumLength(200);
        RuleFor(x => x.TechnicalRequirements).MaximumLength(4000);
        RuleFor(x => x.Note).MaximumLength(4000);
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.CustomerFileUrl).Must(Mainflow2PrintFlowHelper.IsPrintableFile)
            .WithMessage("Chỉ chấp nhận file .stl, .obj hoặc .glb.");
    }
}

public class CreateCustomFilePrintRequestCommandHandler : IRequestHandler<CreateCustomFilePrintRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMainflow2RealtimeNotifier _realtime;

    public CreateCustomFilePrintRequestCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IMainflow2RealtimeNotifier realtime)
    {
        _context = context;
        _user = user;
        _realtime = realtime;
    }

    public async Task<Guid> Handle(CreateCustomFilePrintRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Chỉ khách hàng mới tạo được yêu cầu in 3D.");

        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? "customer";
        var fileUrl = request.CustomerFileUrl.Trim();
        var brief = Mainflow2PrintFlowHelper.BuildPrintBriefNote(
            request.PrintPriority,
            request.Quantity,
            request.PrintSize,
            request.Note);

        var dw = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(request.Title)
                ? $"In từ file ({Path.GetFileName(fileUrl)})"
                : request.Title!.Trim(),
            SourceType = SourceTypes.CustomFilePrintMainflow2,
            CustomerId = customer.Id,
            Status = Mainflow2DesignWorkStatuses.Submitted,
            RequirementBrief = string.IsNullOrWhiteSpace(brief) ? null : brief,
            BaseImageUrl = fileUrl,
            QuoteRevision = 0,
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
            FileUrl = fileUrl,
            VersionNumber = 1,
            Tilte = "File khách upload",
            IsPreviewable = Mainflow2PrintFlowHelper.IsPreviewableFile(fileUrl),
            IsApproved = false,
            IsPrintable = true,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        });

        var metadata = new
        {
            printPriority = request.PrintPriority,
            quantity = request.Quantity,
            printSize = request.PrintSize,
            technicalRequirements = request.TechnicalRequirements,
            customerFileUrl = fileUrl
        };
        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = dw.Id,
            AccountId = accountId,
            Content = string.IsNullOrWhiteSpace(brief)
                ? "Tôi đã tải lên file 3D và mong nhận được báo giá."
                : brief,
            Metadata = JsonSerializer.Serialize(metadata),
            LogType = Mainflow2DesignLogTypes.CustomerMessage,
            IsAI = false,
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
            customerFileUrl = fileUrl
        }, cancellationToken);

        return dw.Id;
    }
}
