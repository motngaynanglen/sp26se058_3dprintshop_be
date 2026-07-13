using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignWorks.Commands;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.AiDesign.Commands;

/// <summary>
/// Đăng ký GLB từ AI — tạo DesignWork loại PRINT_SERVICE với file AI.
/// Tương đương Quick Print nhưng source là AI generated.
/// </summary>
[Authorize(Roles = Roles.CUSTOMER)]
public record RegisterAiGeneratedDesignWorkCommand : IRequest<Guid>
{
    public string? Name { get; init; }
    public required string ModelFileUrl { get; init; }
    public string? SourceImageUrl { get; init; }
    public string? Prompt { get; init; }
}

public class RegisterAiGeneratedDesignWorkCommandValidator : AbstractValidator<RegisterAiGeneratedDesignWorkCommand>
{
    public RegisterAiGeneratedDesignWorkCommandValidator()
    {
        RuleFor(x => x.ModelFileUrl).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Name).MaximumLength(500);
        RuleFor(x => x.SourceImageUrl).MaximumLength(1000);
        RuleFor(x => x.Prompt).MaximumLength(4000);
    }
}

public class RegisterAiGeneratedDesignWorkCommandHandler : IRequestHandler<RegisterAiGeneratedDesignWorkCommand, Guid>
{
    private readonly ISender _sender;

    public RegisterAiGeneratedDesignWorkCommandHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<Guid> Handle(RegisterAiGeneratedDesignWorkCommand request, CancellationToken cancellationToken)
    {
        // Delegate sang AddFilesToQuickPrintCommand (Quick Print flow)
        var result = await _sender.Send(new AddFilesToQuickPrintCommand
        {
            ProjectName = request.Name ?? "Mô hình AI",
            Description = request.Prompt,
            FileUrls = new List<string> { request.ModelFileUrl },
            Note = request.SourceImageUrl != null
                ? $"AI generated. Source image: {request.SourceImageUrl}"
                : "AI generated model",
        }, cancellationToken);

        return result.Id;
    }
}
