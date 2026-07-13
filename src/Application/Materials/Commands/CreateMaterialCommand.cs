using System.ComponentModel;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

[Authorize(Roles = Roles.MANAGER)]
public record CreateMaterialCommand : IRequest<MaterialDTO>
{
    [DefaultValue("PLA")]
    public string Name { get; init; } = null!;

    [DefaultValue("Nhựa PLA phổ biến cho in 3D, dễ in và phù hợp sản phẩm trang trí.")]
    public string Description { get; init; } = null!;

    [DefaultValue(2.5)]
    public decimal? BaseCostPerGram { get; init; }

    [DefaultValue(5.0)]
    public decimal? TotalServiceCostPerGram { get; init; }

    [DefaultValue("2026-05-22")]
    public DateTime? EffectiveDate { get; init; }
}

public class CreateMaterialCommandValidator : AbstractValidator<CreateMaterialCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateMaterialCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Tên vật liệu không được để trống.")
            .MaximumLength(100).WithMessage("Tên vật liệu không vượt quá 100 ký tự.")
            .MustAsync(BeUniqueName).WithMessage("Tên vật liệu này đã tồn tại trong hệ thống.");

        RuleFor(v => v.Description)
            .NotEmpty().WithMessage("Mô tả vật liệu không được để trống.")
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.");

        RuleFor(v => v)
            .Must(HaveFullPriceInfoOrNoPriceInfo)
            .WithMessage("Nếu nhập giá vật liệu thì phải nhập đủ giá gốc, giá dịch vụ và ngày hiệu lực.");

        RuleFor(v => v.BaseCostPerGram)
            .GreaterThan(0).When(v => v.BaseCostPerGram.HasValue)
            .WithMessage("Giá nhập phải lớn hơn 0.");

        RuleFor(v => v.TotalServiceCostPerGram)
            .GreaterThanOrEqualTo(v => v.BaseCostPerGram!.Value)
            .When(v => v.BaseCostPerGram.HasValue && v.TotalServiceCostPerGram.HasValue)
            .WithMessage("Giá dịch vụ phải lớn hơn hoặc bằng giá nhập.");

        RuleFor(v => v.EffectiveDate)
            .Must(date => !date.HasValue || date.Value.Date >= DateTime.Today)
            .WithMessage("Ngày hiệu lực không được là ngày trong quá khứ.");
    }

    public async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLower();
        return !await _context.Materials
            .AnyAsync(m => m.Name.ToLower() == normalizedName, cancellationToken);
    }

    private static bool HaveFullPriceInfoOrNoPriceInfo(CreateMaterialCommand command)
    {
        var providedCount = new[]
        {
            command.BaseCostPerGram.HasValue,
            command.TotalServiceCostPerGram.HasValue,
            command.EffectiveDate.HasValue
        }.Count(x => x);

        return providedCount is 0 or 3;
    }
}

public class CreateMaterialCommandHandler : IRequestHandler<CreateMaterialCommand, MaterialDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public CreateMaterialCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<MaterialDTO> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
    {
        var now = CoreHelper.SystemTimeNow;
        bool hasPriceInfo = request.BaseCostPerGram.HasValue
            && request.TotalServiceCostPerGram.HasValue
            && request.EffectiveDate.HasValue;

        var newMaterial = new Material
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            IsActive = hasPriceInfo,
            Created = now,
            CreatedBy = _user.Username,
            LastModified = now,
            LastModifiedBy = _user.Username,
        };

        _context.Materials.Add(newMaterial);

        if (hasPriceInfo)
        {
            var priceHistory = new MaterialPriceHistory
            {
                Material = newMaterial,
                BaseCostPerGram = request.BaseCostPerGram!.Value,
                TotalServiceCostPerGram = request.TotalServiceCostPerGram!.Value,
                EffectiveDate = request.EffectiveDate!.Value,
                IsCurrent = true,
                Created = now,
                CreatedBy = _user.Username,
                LastModified = now,
                LastModifiedBy = _user.Username
            };
            _context.MaterialPriceHistories.Add(priceHistory);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new CreateFailureException(nameof(Material), ex.Message);
        }

        return _mapper.Map<MaterialDTO>(newMaterial);
    }
}
