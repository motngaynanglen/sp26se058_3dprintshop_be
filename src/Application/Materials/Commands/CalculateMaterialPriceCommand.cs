using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

public record MaterialEstimationResultDto(
    Guid MaterialId,
    string MaterialName,
    double WeightInGrams,
    decimal CurrentBaseCostPerGram,
    decimal CurrentServiceCostPerGram,
    decimal TotalBaseCost,
    decimal FinalPrice,
    string Currency = "VND"
);

[Authorize(Roles = Roles.StaffOrManager)]
public record CalculateMaterialPriceCommand: IRequest<MaterialEstimationResultDto>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid MaterialId { get; init; }

    [DefaultValue(10.0)]
    [Range(0.01, double.MaxValue, ErrorMessage = "Khối lượng phải lớn hơn 0 gram.")]
    public double WeightInGrams { get; init; }
}

public class CalculateMaterialPriceCommandValidator : AbstractValidator<CalculateMaterialPriceCommand>
{
    public CalculateMaterialPriceCommandValidator()
    {
        RuleFor(v => v.MaterialId)
            .NotEmpty().WithMessage("ID vật liệu không được để trống.")
            .NotEqual(Guid.Empty).WithMessage("ID vật liệu không hợp lệ.");

        RuleFor(v => v.WeightInGrams)
            .GreaterThan(0).WithMessage("Khối lượng vật liệu phải lớn hơn 0 gram.")
            .LessThanOrEqualTo(100000).WithMessage("Khối lượng vượt quá giới hạn cho phép một lần tính (100kg)."); 
    }
}

public class CalculateMaterialPriceCommandHandler : IRequestHandler<CalculateMaterialPriceCommand, MaterialEstimationResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPricingEngine _pricingEngine;

    public CalculateMaterialPriceCommandHandler(IApplicationDbContext context, IPricingEngine pricingEngine)
    {
        _context = context;
        _pricingEngine = pricingEngine;
    }

    public async Task<MaterialEstimationResultDto> Handle(CalculateMaterialPriceCommand request, CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .Include(m => m.PriceHistories)
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId, cancellationToken);

        if (material == null)
        {
            throw new DataNotFoundException(nameof(Material), request.MaterialId);
        }

        if (!material.IsActive)
        {
            throw new BusinessException("Vật liệu đang tạm ngưng hoạt động, không thể tính giá.");
        }

        var currentPrice = material.PriceHistories.FirstOrDefault(p => p.IsCurrent);
        if (currentPrice == null)
        {
            throw new BusinessException("Vật liệu chưa có giá hiện hành, không thể tính giá.");
        }

        decimal baseCostPerGram = currentPrice.BaseCostPerGram;
        decimal serviceCostPerGram = currentPrice.TotalServiceCostPerGram;
        decimal weightDecimal = (decimal)request.WeightInGrams;

        decimal totalBaseCost = baseCostPerGram * weightDecimal;

        // Route through IPricingEngine — markup=0 for this raw estimation endpoint (no markup applied)
        decimal finalPrice = _pricingEngine.CalculatePrintCost(weightDecimal, serviceCostPerGram, 0);

        return new MaterialEstimationResultDto(
            MaterialId: material.Id,
            MaterialName: material.Name,
            WeightInGrams: request.WeightInGrams,
            CurrentBaseCostPerGram: baseCostPerGram,
            CurrentServiceCostPerGram: serviceCostPerGram,
            TotalBaseCost: Math.Round(totalBaseCost, 2),
            FinalPrice: Math.Round(finalPrice, 2)
        );
    }
}
