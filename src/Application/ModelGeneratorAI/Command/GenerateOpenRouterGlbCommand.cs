using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.ModelGeneratorAI.Command;

public record GenerateOpenRouterGlbCommand : IRequest<string>
{
    [Required(ErrorMessage = "Vui lòng nhập prompt mô tả mô hình.")]
    public string Prompt { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ảnh tham chiếu.")]
    public IFormFile? Image { get; init; }
}

public class GenerateOpenRouterGlbCommandHandler : IRequestHandler<GenerateOpenRouterGlbCommand, string>
{
    private readonly IOpenRouterGlbGenerationService _service;
    private readonly IGlbPublicUrlService _glbPublish;

    public GenerateOpenRouterGlbCommandHandler(
        IOpenRouterGlbGenerationService service,
        IGlbPublicUrlService glbPublish)
    {
        _service = service;
        _glbPublish = glbPublish;
    }

    public async Task<string> Handle(GenerateOpenRouterGlbCommand request, CancellationToken cancellationToken)
    {
        if (request.Image is null || request.Image.Length == 0)
            throw new ArgumentException("Ảnh đầu vào không hợp lệ.");
        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new ArgumentException("Prompt không được rỗng.");

        using var ms = new MemoryStream();
        await request.Image.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();
        var imageBase64 = Convert.ToBase64String(bytes);
        var mime = string.IsNullOrWhiteSpace(request.Image.ContentType) ? "image/png" : request.Image.ContentType;

        var glbBytes = await _service.GenerateGlbAsync(request.Prompt, imageBase64, mime, cancellationToken);
        if (glbBytes is null || glbBytes.Length == 0)
            throw new InvalidOperationException("OpenRouter trả về dữ liệu GLB rỗng.");

        return await _glbPublish.PublishGlbAsync(glbBytes, "openrouter-models", cancellationToken);
    }
}
