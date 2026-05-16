using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignLogs.Commands;

[Authorize(Roles = Roles.StaffOrManager)]
public record CreateVersionUpdateLogCommand : IRequest<DesignLogDTO>
{
    public Guid DesignWorkId { get; init; }

    [DefaultValue("Đã cập nhật bản vẽ 3D chi tiết phần khớp nối")]
    public string? Content { get; init; }

    // Các trường dành cho DesignVersionHistory
    [DefaultValue("Bản vẽ kỹ thuật v2")]
    public string? Title { get; init; }

    [DefaultValue("https://storage.com/design-v2.stl")]
    public string FileUrl { get; init; } = null!;

    [DefaultValue(true)]
    public bool IsPreviewable { get; init; } = true;

    [DefaultValue(false)]
    public bool IsPrintable { get; init; } = false;
}

public class CreateVersionUpdateLogCommandHandler : IRequestHandler<CreateVersionUpdateLogCommand, DesignLogDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CreateVersionUpdateLogCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignLogDTO> Handle(CreateVersionUpdateLogCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra sự tồn tại của DesignWork
        var designWork = await _context.DesignWorks
            .Include(dw => dw.VersionHistories)
            .FirstOrDefaultAsync(dw => dw.Id == request.DesignWorkId, cancellationToken);

        if (designWork == null)
            throw new DataNotFoundException("Không tìm thấy công việc thiết kế với mã " + request.DesignWorkId);

        var currentUserId = _user.Id != null ? Guid.Parse(_user.Id) : Guid.Empty;

        // 2. Tạo DesignLog (VERSION_UPDATE)
        var newLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = request.DesignWorkId,
            AccountId = currentUserId,
            Content = request.Content,
            LogType = "VERSION_UPDATE",
            IsAI = false,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            //IsRead = false
        };

        // 3. Tính toán VersionNumber tiếp theo
        int nextVersion = (designWork.VersionHistories.Max(v => (int?)v.VersionNumber) ?? 0) + 1;

        // 4. Tạo DesignVersionHistory liên kết với Log trên
        var newVersion = new DesignVersionHistory
        {
            Id = Guid.NewGuid(),
            DesignWorkId = request.DesignWorkId,
            DesignLogId = newLog.Id, // Foreign Key quan trọng
            UploaderId = currentUserId,
            Tilte = request.Title ?? $"Phiên bản {nextVersion}",
            FileUrl = request.FileUrl,
            VersionNumber = nextVersion,
            IsPreviewable = request.IsPreviewable,
            IsApproved = false, // Luôn mặc định false khi mới upload
            IsPrintable = request.IsPrintable,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username
        };

        _context.DesignLogs.Add(newLog);
        _context.DesignVersionHistorys.Add(newVersion);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new CreateFailureException("log cập nhật phiên bản", ex.Message);
        }

        // Trả về DTO của Log (đã bao gồm thông tin vừa tạo)
        return _mapper.Map<DesignLogDTO>(newLog);
    }
}
