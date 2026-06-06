using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.ModelGeneratorAI.Command;

// ==================== COMMAND ====================
public record GenerateModelCommand : IRequest<string>
{
    [Required(ErrorMessage = "Vui lòng chọn ảnh để tạo mô hình")]
    public IFormFile? Image { get; init; }
}

// ==================== HANDLER ====================
public class GenerateModelCommandHandler : IRequestHandler<GenerateModelCommand, string>
{
    private readonly IAIService _aiService;
    private readonly IGlbPublicUrlService _glbPublish;

    public GenerateModelCommandHandler(IAIService aiService, IGlbPublicUrlService glbPublish)
    {
        _aiService = aiService;
        _glbPublish = glbPublish;
    }

    public async Task<string> Handle(GenerateModelCommand request, CancellationToken cancellationToken)
    {
        if (request.Image == null || request.Image.Length == 0)
            throw new ArgumentException("Ảnh đầu vào không hợp lệ");

        // 1. Chuyển ảnh đầu vào (IFormFile) sang Base64 để gửi Request cho AI
        string imageBase64 = await ConvertFileToBase64Async(request.Image);

        // 2. Gọi AI để sinh mô hình. 
        // Kết quả nhận về là byte[] (nội dung file .glb)
        byte[] glbData = await _aiService.GenerateModelAsync(imageBase64);

        if (glbData == null || glbData.Length == 0)
            throw new Exception("AI Service trả về dữ liệu trống hoặc không hợp lệ.");

        return await _glbPublish.PublishGlbAsync(glbData, "ai-models", cancellationToken);
    }

    private static async Task<string> ConvertFileToBase64Async(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return Convert.ToBase64String(memoryStream.ToArray());
    }
}
