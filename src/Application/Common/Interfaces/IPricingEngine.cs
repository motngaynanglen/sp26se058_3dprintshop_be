namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

/// <summary>
/// Single source of truth for all print-cost calculations.
/// Every pricing logic in the codebase routes through this interface.
/// </summary>
public interface IPricingEngine
{
    /// <summary>
    /// Core formula: weight × pricePerGram × (1 + markup/100)
    /// Used by TechnicalDraft and DesignVariant pricing.
    /// </summary>
    decimal CalculatePrintCost(decimal weightGrams, decimal pricePerGram, decimal markupPercent);

    /// <summary>
    /// Extended formula: adds machine time cost on top of material cost.
    /// printTimeHours = EstimatedPrintTimePerUnit converted from minutes.
    /// machineRatePerHour = cost per hour of machine operation.
    /// Formula: ((weight × pricePerGram) + (hours × machineRate)) × (1 + markup/100)
    /// </summary>
    decimal CalculatePrintCostWithTime(
        decimal weightGrams,
        decimal pricePerGram,
        decimal markupPercent,
        decimal printTimeHours,
        decimal machineRatePerHour);

    /// <summary>
    /// Convenience: resolves the current active material price from DB,
    /// then calls CalculatePrintCost.
    /// Returns null if no active price found — caller is responsible for throwing.
    /// </summary>
    Task<decimal?> EstimatePrintCostAsync(
        Guid materialId,
        decimal weightGrams,
        decimal markupPercent,
        CancellationToken ct = default);
}
