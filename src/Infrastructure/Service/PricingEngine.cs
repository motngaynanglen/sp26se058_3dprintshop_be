using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Infrastructure.Data;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

/// <summary>
/// Concrete implementation of IPricingEngine.
/// All pricing arithmetic is pure: no side effects, no DB writes.
/// Only EstimatePrintCostAsync reads DB to resolve current material price.
/// </summary>
public class PricingEngine : IPricingEngine
{
    private readonly IApplicationDbContext _context;

    public PricingEngine(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public decimal CalculatePrintCost(decimal weightGrams, decimal pricePerGram, decimal markupPercent)
    {
        return weightGrams * pricePerGram * (1 + markupPercent / 100m);
    }

    /// <inheritdoc/>
    public decimal CalculatePrintCostWithTime(
        decimal weightGrams,
        decimal pricePerGram,
        decimal markupPercent,
        decimal printTimeHours,
        decimal machineRatePerHour)
    {
        var materialCost = weightGrams * pricePerGram;
        var machineCost = printTimeHours * machineRatePerHour;
        return (materialCost + machineCost) * (1 + markupPercent / 100m);
    }

    /// <inheritdoc/>
    public async Task<decimal?> EstimatePrintCostAsync(
        Guid materialId,
        decimal weightGrams,
        decimal markupPercent,
        CancellationToken ct = default)
    {
        // Rule: always use IsCurrent=true AND Material.IsActive=true
        // Always use TotalServiceCostPerGram — never BaseCostPerGram for pricing
        var pricePerGram = await _context.MaterialPriceHistories
            .AsNoTracking()
            .Where(p => p.MaterialId == materialId && p.IsCurrent && p.Material.IsActive)
            .Select(p => (decimal?)p.TotalServiceCostPerGram)
            .FirstOrDefaultAsync(ct);

        if (pricePerGram == null)
            return null;

        return CalculatePrintCost(weightGrams, pricePerGram.Value, markupPercent);
    }
}
