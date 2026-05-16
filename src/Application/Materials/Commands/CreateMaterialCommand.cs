using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

[Authorize(Roles = Roles.StaffOrManager)]
public record CreateMaterialCommand : IRequest<MaterialDTO>
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    // Cho phép nullable để linh hoạt
    public decimal? BaseCostPerGram { get; init; }
    public decimal? TotalServiceCostPerGram { get; init; }
    public DateTime? EffectiveDate { get; init; }
}
public class CreateMaterialCommandValidator : AbstractValidator<CreateMaterialCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateMaterialCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        // 1. Kiểm tra Tên vật liệu
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Tên vật liệu không được để trống.")
            .MaximumLength(100).WithMessage("Tên vật liệu không vượt quá 100 ký tự.")
            .MustAsync(BeUniqueName).WithMessage("Tên vật liệu này đã tồn tại trong hệ thống.");

        // 2. Kiểm tra Mô tả
        RuleFor(v => v.Description)
            .NotEmpty().WithMessage("Mô tả vật liệu không được để trống.")
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.");

        // 3. Kiểm tra đơn giá gốc
        RuleFor(v => v.BaseCostPerGram)
            .GreaterThan(0).WithMessage("Giá nhập phải lớn hơn 0.");

        // 4. Kiểm tra phí dịch vụ
        RuleFor(v => v.TotalServiceCostPerGram)
            .GreaterThan(v => v.BaseCostPerGram).WithMessage("Phí dịch vụ phải cao hơn giá nhập.");

        // 5. Kiểm tra Ngày hiệu lực
        RuleFor(v => v.EffectiveDate)
            .NotEmpty().WithMessage("Ngày hiệu lực không được để trống.")
            .Must(date => date >= DateTime.Today).WithMessage("Ngày hiệu lực không được là ngày trong quá khứ.");
    }

    // Hàm kiểm tra trùng tên bất đồng bộ
    public async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        return !await _context.Materials
            .AnyAsync(m => m.Name.ToLower() == name.ToLower(), cancellationToken);
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
        bool hasPriceInfo = request.BaseCostPerGram.HasValue && request.EffectiveDate.HasValue;

        var newMaterial = new Material
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = hasPriceInfo,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };
        _context.Materials.Add(newMaterial);

        if (hasPriceInfo)
        {
            var priceHistory = new MaterialPriceHistory
            {
                Material = newMaterial,
                BaseCostPerGram = request.BaseCostPerGram!.Value,
                TotalServiceCostPerGram = request.TotalServiceCostPerGram ?? 0,
                EffectiveDate = request.EffectiveDate!.Value,
                IsCurrent = true,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username
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

