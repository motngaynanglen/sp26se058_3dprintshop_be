using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayOS.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Application.TechnicalDrafts.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;
using static Amazon.S3.Util.S3EventNotification;

namespace sp26se058_3dprintshop_be.Application.TechnicalDrafts.Commands;
[Authorize(Roles = Roles.StaffOrManager)]
public record CreateTechnicalDraftCommand : IRequest<object>
{
    public Guid DesignVersionHistoryId { get; init; }
    public Guid MaterialId { get; init; }

    [DefaultValue(20)]
    public int InfillDensity { get; init; }

    [DefaultValue(0.2)]
    public decimal LayerHeight { get; init; }

    [DefaultValue(150.5)]
    public decimal EstimatedWeightPerUnit { get; init; }

    [DefaultValue(120)]
    public decimal? EstimatedPrintTimePerUnit { get; init; }
    [DefaultValue(null)]
    public decimal? UnitPrice { get; set; }
    [DefaultValue(10)]
    public decimal MarkupPercentage { get; init; }
    [DefaultValue("")]
    public string? TechnicalNote { get; set; }
}
public class CreateTechnicalDraftCommandValidator : AbstractValidator<CreateTechnicalDraftCommand>
{
    public CreateTechnicalDraftCommandValidator()
    {
        RuleFor(v => v.DesignVersionHistoryId).NotEmpty().WithMessage("Phiên bản thiết kế không được để trống");
        RuleFor(v => v.MaterialId).NotEmpty().WithMessage("Vật liệu không được để trống");
        RuleFor(v => v.InfillDensity).InclusiveBetween(0, 100).WithMessage("Infill phải từ 0-100%");
        RuleFor(v => v.LayerHeight).GreaterThan(0).WithMessage("Độ dày lớp in phải lớn hơn 0");
        RuleFor(v => v.EstimatedWeightPerUnit).GreaterThan(0).WithMessage("Khối lượng phải lớn hơn 0");
        RuleFor(v => v.MarkupPercentage).GreaterThanOrEqualTo(0).WithMessage("Phụ thu không được âm");
    }

}
public class CreateTechnicalDraftCommandHandler : IRequestHandler<CreateTechnicalDraftCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;
    public CreateTechnicalDraftCommandHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }
    public async Task<object> Handle(CreateTechnicalDraftCommand request, CancellationToken ct)
    {
        // 1. Kiểm tra Version và DesignWork liên quan (Business Validation)
        var version = await _context.DesignVersionHistorys
            .Include(v => v.DesignWork)
            .FirstOrDefaultAsync(v => v.Id == request.DesignVersionHistoryId, ct);

        if (version == null)
            throw new DataNotFoundException(nameof(DesignVersionHistory), request.DesignVersionHistoryId);

        var currentMaterialPrice = await _context.Materials.AsNoTracking()
                .Where(m => m.Id == request.MaterialId)
                .ProjectTo<MaterialDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(ct);

        if (currentMaterialPrice == null)
        {
            throw new DataNotFoundException(nameof(Material), request.MaterialId);
        }
        decimal unitPrice = request.EstimatedWeightPerUnit * currentMaterialPrice.TotalServiceCostPerGram;
                                

        // 2. Kiểm tra Logic trạng thái (State Machine Validation)
        // Cho phép tạo Draft nếu dự án đã thanh toán (không còn ở SKETCHING/PENDING)
        var allowedStatuses = new[] {
            DesignWorkStatus.InProgress,
            DesignWorkStatus.Reviewing,
            DesignWorkStatus.Completed
        };
        if (!allowedStatuses.Contains(version.DesignWork.Status))
        {
            throw new BusinessException($"Dự án đang ở trạng thái {version.DesignWork.Status}, chưa thể tạo bản thông số kỹ thuật.", ResponseCodeConstants.VAL_INVALID_STATE);
        }

        // 3.Khởi tạo Entity TechnicalDraft
        var entity = new TechnicalDraft
        {
            Id = Guid.NewGuid(),
            DesignVersionHistoryId = request.DesignVersionHistoryId,
            MaterialId = request.MaterialId,
            InfillDensity = request.InfillDensity,
            LayerHeight = request.LayerHeight,
            EstimatedWeightPerUnit = request.EstimatedWeightPerUnit,
            EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit,
            UnitPrice = request.UnitPrice ?? unitPrice,
            MarkupPercentage = request.MarkupPercentage,
            TechnicalNote = request.TechnicalNote,

            // Audit fields
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username 
        };

        // 4. Đồng bộ hóa với DesignWork
        // Gán Draft này làm kết quả kỹ thuật mới nhất
        version.DesignWork.ResultDraftId = entity.Id;

        // CHUYỂN TRẠNG THÁI: Progressing -> Reviewing
        // Nếu dự án đã Completed (đã chốt xong xuôi), chúng ta giữ nguyên trạng thái Completed
        if (version.DesignWork.Status == DesignWorkStatus.InProgress)
        {
            version.DesignWork.Status = DesignWorkStatus.Reviewing;
        }
        version.DesignWork.LastModified = CoreHelper.SystemTimeNow;
        version.DesignWork.LastModifiedBy = _user.Username;

        // 5. Tự động tạo Log thông báo cho Khách hàng
        var systemLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = version.DesignWorkId,
            Content = version.DesignWork.Status == DesignWorkStatus.Completed
            ? $"[Sản xuất] Cập nhật thông số kỹ thuật mới cho phiên bản in."
            : $"Đã tạo bản nháp kỹ thuật và báo giá dự kiến.",
            LogType = DesignLogType.VersionUpdate,
            Created = CoreHelper.SystemTimeNow
        };

        _context.TechnicalDrafts.Add(entity);
        _context.DesignLogs.Add(systemLog);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new CreateFailureException(nameof(TechnicalDraft), $"{ex.InnerException?.Message ?? ex.Message}");
        }

        return _mapper.Map<TechnicalDraftDTO>(entity);
    }
}
