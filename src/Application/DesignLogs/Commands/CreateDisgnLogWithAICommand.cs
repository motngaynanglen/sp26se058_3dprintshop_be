using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Org.BouncyCastle.Asn1.Cms;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignLogs.Commands;

public record CreateDisgnLogWithAICommand : IRequest<DesignLogDTO>
{
    public Guid? DesignWorkId { get; init; } = null;

    [JsonIgnore]
    [Required(ErrorMessage = "Vui lòng chọn ảnh để tạo mô hình")]
    public required IFormFile Image { get; init; }
}

public class CreateDesignLogWithAICommandHandler : IRequestHandler<CreateDisgnLogWithAICommand, DesignLogDTO>
{
    private readonly IAIService _aiService;
    private readonly IBackblazeB2Service _b2Service;
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    public CreateDesignLogWithAICommandHandler(IAIService aiService, IBackblazeB2Service b2Service, IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _aiService = aiService;
        _b2Service = b2Service;
        _context = context;
        _mapper = mapper;
        _user = user;
    }
    public async Task<DesignLogDTO> Handle(CreateDisgnLogWithAICommand request, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin User hiện tại
        var currentUserIdStr = _user.Id;
        if (string.IsNullOrEmpty(currentUserIdStr)) throw new UnauthorizedAccessException();
        var userId = Guid.Parse(currentUserIdStr);

        // Tìm Customer tương ứng với AccountId
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.AccountId == userId, cancellationToken);
        if (customer == null) throw new ForbiddenAccessException("Người dùng không phải khách hàng.");

        // 2. Fake các đường dẫn tệp tin
        string fakeImageUrl = "https://b2.example.com/uploads/sketch_sample.png";
        string fakeModelUrl = "https://b2.example.com/models/result_v1.glb";

        DesignWork? designWork;

        // 3. Xử lý logic DesignWork dựa trên DesignWorkId truyền vào
        if (request.DesignWorkId.HasValue && request.DesignWorkId != Guid.Empty)
        {
            designWork = await _context.DesignWorks
                .FirstOrDefaultAsync(dw => dw.Id == request.DesignWorkId && dw.CustomerId == customer.Id, cancellationToken);

            if (designWork == null)
                throw new NotFoundException(nameof(DesignWork), request.DesignWorkId.Value.ToString());
        }
        else
        {
            // Tạo mới hoàn toàn (Fresh Start)
            designWork = new DesignWork
            {
                Id = Guid.NewGuid(),
                Name = $"Dự án AI - {DateTime.Now:dd/MM/yyyy HH:mm}",
                CustomerId = customer.Id,
                Status = DesignWorkStatus.Sketching, // Trạng thái ban đầu 
                                                     // RelationshipType = "ORIGINAL", // Thêm trường này nếu Entity của bạn đã cập nhật theo doc
                BaseImageUrl = fakeImageUrl,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "System"
            };
            _context.DesignWorks.Add(designWork);
        }

        // 4. Tạo DesignLog lưu lịch sử vọc AI
        var designLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = designWork.Id,
            AccountId = userId,
            IsAI = true, // Đánh dấu do AI tạo 
            LogType = "AI_GEN", // Loại log dành cho khách vọc AI 
            Content = "AI đã tạo mẫu 3D từ hình ảnh của bạn.",
            // Lưu trữ mảng link trong Metadata dưới dạng JSON 

            Metadata = JsonSerializer.Serialize(new
            {
                SourceImage = fakeImageUrl,
                ModelResult = fakeModelUrl
            }),
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = "AI_System"
        };

        _context.DesignLogs.Add(designLog);

        // 5. Lưu xuống Database
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception("Lỗi khi lưu log AI: " + ex.Message);
        }

        // 6. Trả về DTO cho Client
        return _mapper.Map<DesignLogDTO>(designLog);
    }
}
