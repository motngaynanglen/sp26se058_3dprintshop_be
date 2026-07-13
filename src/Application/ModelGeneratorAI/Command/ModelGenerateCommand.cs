using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;
using System.Text.Json;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.ModelGeneratorAI.Command;

// ==================== COMMAND ====================
[Authorize(Roles = Roles.CUSTOMER)]
public record GenerateModelCommand : IRequest<string>
{
    [Required(ErrorMessage = "Vui lòng chọn ảnh để tạo mô hình")]
    public IFormFile? Image { get; init; }

    public Guid? DesignWorkId { get; init; }

    public string? Prompt { get; init; }
}

// ==================== HANDLER ====================
public class GenerateModelCommandHandler : IRequestHandler<GenerateModelCommand, string>
{
    private readonly IAIService _aiService;
    private readonly IBackblazeB2Service _b2Service;
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GenerateModelCommandHandler(
        IAIService aiService,
        IBackblazeB2Service b2Service,
        IApplicationDbContext context,
        IUser user)
    {
        _aiService = aiService;
        _b2Service = b2Service;
        _context = context;
        _user = user;
    }

    public async Task<string> Handle(GenerateModelCommand request, CancellationToken cancellationToken)
    {
        if (request.Image == null || request.Image.Length == 0)
            throw new BusinessException("Ảnh đầu vào không hợp lệ.");

        // 1. Chuyển ảnh đầu vào (IFormFile) sang Base64 để gửi Request cho AI
        string imageBase64 = await ConvertFileToBase64Async(request.Image);

        // 2. Gọi AI để sinh mô hình. 
        // Kết quả nhận về là byte[] (nội dung file .glb)
        byte[] glbData = await _aiService.GenerateModelAsync(imageBase64);

        if (glbData == null || glbData.Length == 0)
            throw new BusinessException("Dịch vụ AI trả về dữ liệu trống hoặc không hợp lệ.");

        // 3. Upload mảng byte trực tiếp lên Backblaze B2 và trả về URL
        string glbUrl = await _b2Service.UploadGlbAsync(glbData);

        if (request.DesignWorkId.HasValue && request.DesignWorkId.Value != Guid.Empty)
        {
            var designWorkExists = await _context.DesignWorks
                .AnyAsync(x => x.Id == request.DesignWorkId.Value, cancellationToken);

            if (!designWorkExists)
            {
                throw new DataNotFoundException(nameof(DesignWork), request.DesignWorkId.Value);
            }

            var userId = _user.Id.ToGuid();
            var customerOwnsDesignWork = await _context.Customers
                .AsNoTracking()
                .AnyAsync(x => x.AccountId == userId && x.DesignWorks.Any(dw => dw.Id == request.DesignWorkId.Value), cancellationToken);

            if (!customerOwnsDesignWork)
            {
                throw new ForbiddenAccessException();
            }

            var aiLog = new DesignLog
            {
                Id = Guid.NewGuid(),
                DesignWorkId = request.DesignWorkId.Value,
                AccountId = _user.Id != null ? Guid.Parse(_user.Id) : null,
                IsAI = true,
                Content = request.Prompt ?? "AI generated a 3D model from the uploaded image.",
                Metadata = JsonSerializer.Serialize(new List<string> { glbUrl }),
                LogType = DesignLogType.AiGen,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "AI"
            };

            _context.DesignLogs.Add(aiLog);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return glbUrl;
    }

    private static async Task<string> ConvertFileToBase64Async(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return Convert.ToBase64String(memoryStream.ToArray());
    }
}
