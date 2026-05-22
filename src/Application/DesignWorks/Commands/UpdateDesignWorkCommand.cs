using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;
using AutoMapper;
using System.ComponentModel;
using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

[Authorize(Roles = Roles.StaffOrManager)]
public record UpdateDesignWorkCommand : IRequest<DesignWorkDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }

    [DefaultValue("Tên yêu cầu mới")]
    public string? Name { get; init; }

    [DefaultValue("InProgress")]
    public string? Status { get; init; }

    [DefaultValue("https://example.com/new-base-image.png")]
    public string? BaseImageUrl { get; init; }
    [DefaultValue(null)]
    public Guid? MainAssignedStaffId { get; init; }

    // ResultDraftId thường được cập nhật khi thiết kế đã hoàn tất và sẵn sàng in
    [DefaultValue(null)]
    public Guid? ResultDraftId { get; init; }
}

public class UpdateDesignWorkCommandHandler : IRequestHandler<UpdateDesignWorkCommand, DesignWorkDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public UpdateDesignWorkCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignWorkDTO> Handle(UpdateDesignWorkCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.DesignWorks
            .FirstOrDefaultAsync(dw => dw.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new DataNotFoundException(nameof(DesignWork),  request.Id);//"Không tìm thấy công việc thiết kế với id " +
        }

        // 1. Kiểm tra quyền (Ví dụ: Customer chỉ được sửa yêu cầu của chính mình)
        // Lưu ý: Logic này giả định bạn đã có cơ chế lấy Role từ _user
        // if (_user.Role == Roles.CUSTOMER && entity.CreatedBy != _user.Username) throw new ForbiddenAccessException();

        if (entity.IsLocked && (request.Name != null || request.BaseImageUrl != null || request.Status != null || request.MainAssignedStaffId != null || request.ResultDraftId != null))
        {
            throw new BusinessException("Công việc thiết kế đã bị khóa, không thể cập nhật thông tin.");
        }

        var oldStatus = entity.Status;

        if (request.Status != null)
        {
            var currentDef = DesignWorkStatus.All.FirstOrDefault(x => x.Value == entity.Status);
            var targetDef = DesignWorkStatus.All.FirstOrDefault(x => x.Value == request.Status);

            if (targetDef == null)
                throw new BusinessException("Trạng thái công việc thiết kế không hợp lệ.");

            if (request.Status != entity.Status
                && (currentDef?.AllowedNextStatuses == null
                    || !currentDef.AllowedNextStatuses.Contains(request.Status)))
            {
                throw new BusinessException(
                    $"Không thể chuyển trạng thái từ '{entity.Status}' sang '{request.Status}'.");
            }
        }

        // 2. Cập nhật các trường thông tin (Chỉ cập nhật nếu request có giá trị)
        if (request.Name != null) entity.Name = request.Name;
        if (request.BaseImageUrl != null) entity.BaseImageUrl = request.BaseImageUrl;
        if (request.Status != null) entity.Status = request.Status;
        if (request.MainAssignedStaffId != null) entity.MainAssignedStaffId = request.MainAssignedStaffId;
        if (request.ResultDraftId != null) entity.ResultDraftId = request.ResultDraftId;

        // 3. Ghi log tự động khi có sự thay đổi quan trọng (ví dụ đổi trạng thái)
        if (request.Status != null && request.Status != oldStatus)
        {
            var statusLog = new DesignLog
            {
                Id = Guid.NewGuid(),
                DesignWorkId = entity.Id,
                LogType = DesignLogType.StatusChange,
                Content = $"Trạng thái yêu cầu được thay đổi từ {oldStatus} sang {request.Status}",
                AccountId = _user.Id != null ? Guid.Parse(_user.Id) : null,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "SYSTEM"
            };
            _context.DesignLogs.Add(statusLog);
        }

        // 4. Cập nhật thông tin Audit
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException(nameof(DesignWork), ex.Message);
        }

        return _mapper.Map<DesignWorkDTO>(entity);
    }
}
