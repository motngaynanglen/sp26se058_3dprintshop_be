using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using sp26se058_3dprintshop_be.Domain.Utils;

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
public record CalculateMaterialPriceCommand: IRequest<MaterialEstimationResultDto>
{
    [JsonIgnore]
    public Guid MaterialId { get; init; }
    [Range(0.01, double.MaxValue, ErrorMessage = "Weight must be greater than 0 grams.")]
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
            .GreaterThan(0).WithMessage("Khối lượng vật liệu phải lớn hơn 0 grams.")
            .LessThanOrEqualTo(100000).WithMessage("Khối lượng vượt quá giới hạn cho phép một lần tính (100kg)."); 
    }
}
public class CalculateMaterialPriceCommandHandler : IRequestHandler<CalculateMaterialPriceCommand, MaterialEstimationResultDto>
{
    private readonly IApplicationDbContext _context;
    public CalculateMaterialPriceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<MaterialEstimationResultDto> Handle(CalculateMaterialPriceCommand request, CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .Include(m => m.PriceHistories)
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId, cancellationToken);

        if (material == null) throw new DataNotFoundException(nameof(Material), request.MaterialId);
        // 1. Tìm và hạ cờ IsCurrent của giá hiện tại
        var currentPrice = material.PriceHistories.FirstOrDefault(p => p.IsCurrent);
        if (currentPrice == null)
        {
            throw new DataNotFoundException(nameof(Material), request.MaterialId);
        }
        decimal baseCostPerGram = currentPrice.BaseCostPerGram;
        decimal serviceCostPerGram = currentPrice.TotalServiceCostPerGram;
        decimal weightDecimal = (decimal)request.WeightInGrams;

        decimal totalBaseCost = baseCostPerGram * weightDecimal;
        decimal totalServiceCost = serviceCostPerGram * weightDecimal;

        return new MaterialEstimationResultDto(
            MaterialId: material.Id,
            MaterialName: material.Name,
            WeightInGrams: request.WeightInGrams,
            CurrentBaseCostPerGram: baseCostPerGram,
            CurrentServiceCostPerGram: serviceCostPerGram,
            TotalBaseCost: Math.Round(totalBaseCost, 2),
            
            FinalPrice: Math.Round(totalServiceCost, 2)
        );
    }
}
