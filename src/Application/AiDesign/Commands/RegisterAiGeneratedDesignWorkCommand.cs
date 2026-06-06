using sp26se058_3dprintshop_be.Application.Mainflow2.Commands;

namespace sp26se058_3dprintshop_be.Application.AiDesign.Commands;

/// <summary>
/// Đăng ký GLB AI — chuyển sang luồng báo giá KTV (giống flow 2).
/// Dùng <see cref="CreateAiGeneratedPrintRequestCommand"/> nội bộ.
/// </summary>
public record RegisterAiGeneratedDesignWorkCommand : IRequest<Guid>
{
    public string? Name { get; init; }
    public required string ModelFileUrl { get; init; }
    public string? SourceImageUrl { get; init; }
    public string? Prompt { get; init; }

    /// <summary>Giá in (VND) — nếu có thì khách checkout ngay sau khi tạo mẫu; không có thì chờ KTV báo giá.</summary>
    public decimal? QuotedPrice { get; init; }
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

    public Task<Guid> Handle(RegisterAiGeneratedDesignWorkCommand request, CancellationToken cancellationToken) =>
        _sender.Send(new CreateAiGeneratedPrintRequestCommand
        {
            Title = request.Name,
            ModelFileUrl = request.ModelFileUrl,
            SourceImageUrl = request.SourceImageUrl,
            Prompt = request.Prompt,
            QuotedPrice = request.QuotedPrice
        }, cancellationToken);
}
