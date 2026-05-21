using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.ServiceOptions.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]

public record CreateServiceOptionCommand : IRequest<object>
{
    [DefaultValue("CODE_A")]
    public string Code { get; init; } = null!;
    [DefaultValue("Thiết kế quy chuẩn")]
    public string Name { get; init; } = null!;
    [DefaultValue("Dịch vụ thiết kế mẫu 3D theo yêu cầu cơ bản.")]
    public string? Description { get; init; }
    [DefaultValue("DESIGN")]
    public string GroupCode { get; init; } = "GENERAL";
    [DefaultValue("Thiết kế")]
    public string GroupName { get; init; } = "Chung";
    [DefaultValue(ServiceOptionSelectionTypes.Addon)]
    public string SelectionType { get; init; } = ServiceOptionSelectionTypes.Addon;
    [DefaultValue(100)]
    public decimal DefaultPrice { get; init; }
    [DefaultValue(1)]
    public int MinQuantity { get; init; } = 1;
    [DefaultValue(null)]
    public int? MaxQuantity { get; init; }
    [DefaultValue(null)]
    public int? AdjustmentRoundDelta { get; init; }
    [DefaultValue(0)]
    public int SortOrder { get; init; }
}
public class CreateServiceOptionCommandValidator : AbstractValidator<CreateServiceOptionCommand>
{
    public CreateServiceOptionCommandValidator()
    {
        RuleFor(v => v.Code)
            .NotEmpty().WithMessage("Mã tùy chọn không được để trống")
            .MaximumLength(50);

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Tên tùy chọn không được để trống")
            .MaximumLength(100);

        RuleFor(v => v.Description)
            .MaximumLength(500).WithMessage("Mô tả tùy chọn không được vượt quá 500 ký tự");

        RuleFor(v => v.GroupCode)
            .NotEmpty().WithMessage("Mã nhóm tùy chọn không được để trống")
            .MaximumLength(50);

        RuleFor(v => v.GroupName)
            .NotEmpty().WithMessage("Tên nhóm tùy chọn không được để trống")
            .MaximumLength(100);

        RuleFor(v => v.SelectionType)
            .NotEmpty().WithMessage("Loại lựa chọn không được để trống")
            .Must(ServiceOptionSelectionTypes.IsValid)
            .WithMessage($"Loại lựa chọn không hợp lệ. Chỉ chấp nhận: {string.Join(", ", ServiceOptionSelectionTypes.All.Select(x => x.Value))}");

        RuleFor(v => v.DefaultPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Giá mặc định không được nhỏ hơn 0");

        RuleFor(v => v.MinQuantity)
            .GreaterThanOrEqualTo(1).WithMessage("Số lượng tối thiểu phải lớn hơn hoặc bằng 1");

        RuleFor(v => v.MaxQuantity)
            .Must((request, maxQuantity) => maxQuantity == null || maxQuantity >= request.MinQuantity)
            .WithMessage("Số lượng tối đa phải lớn hơn hoặc bằng số lượng tối thiểu");

        RuleFor(v => v.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị không được nhỏ hơn 0");

        RuleFor(v => v.AdjustmentRoundDelta)
            .GreaterThanOrEqualTo(0)
            .When(v => v.AdjustmentRoundDelta.HasValue)
            .WithMessage("Số lượt hiệu chỉnh cộng thêm không được nhỏ hơn 0");

        RuleFor(v => v)
            .Must(v => !IsRevisionGroup(v.GroupCode) || (v.AdjustmentRoundDelta.HasValue && v.AdjustmentRoundDelta.Value > 0))
            .WithMessage("Tùy chọn thuộc nhóm REVISION phải có số lượt hiệu chỉnh cộng thêm lớn hơn 0.");

    }

    private static bool IsRevisionGroup(string? groupCode)
        => string.Equals(groupCode?.Trim(), "REVISION", StringComparison.OrdinalIgnoreCase);
}
public class CreateServiceOptionCommandHandler : IRequestHandler<CreateServiceOptionCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public CreateServiceOptionCommandHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<object> Handle(CreateServiceOptionCommand request, CancellationToken ct)
    {
        var normalizedCode = request.Code.Trim().ToUpper();

        var checkResult = await _context.ServiceOptions.GetDuplicateResultAsync(
                 x => x.Code == normalizedCode,
                 nameof(ServiceOption),
                 nameof(request.Code),
                 normalizedCode,
                 ct: ct);
        checkResult.ThrowIfDuplicate();

        var entity = new ServiceOption
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Description = request.Description,
            GroupCode = request.GroupCode.Trim().ToUpper(),
            GroupName = request.GroupName.Trim(),
            SelectionType = request.SelectionType.Trim().ToUpper(),
            DefaultPrice = request.DefaultPrice,
            MinQuantity = request.MinQuantity,
            MaxQuantity = request.MaxQuantity,
            AdjustmentRoundDelta = request.AdjustmentRoundDelta,
            SortOrder = request.SortOrder,
            IsActive = true,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };

        _context.ServiceOptions.Add(entity);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new CreateFailureException(nameof(ServiceOption), $"{ex.InnerException?.Message ?? ex.Message}");
        }

        return _mapper.Map<ServiceOptionDTO>(entity);
    }
}
