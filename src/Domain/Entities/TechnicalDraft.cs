using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class TechnicalDraft : BaseAuditableEntity
{
    public string? Name { get; set; } = string.Empty;
    public decimal Price { get; set; } = decimal.Zero;
    public string? PreviewModelUrl { get; set; }

    public int InfillDensity { get; set; } // %
    public decimal LayerHeight { get; set; } // mm

    public decimal EstimatedWeightPerUnit { get; set; } // Gram
    public decimal? EstimatedPrintTimePerUnit { get; set; } // Giờ
    public decimal MarkupPercentage { get; set; } = 0;

    public string? TechnicalNote { get; set; }
    public Guid DesignVersionHistoryId { get; set; }
    public Guid MaterialId { get; set; }
    public virtual Material Material { get; set; } = null!;
    public virtual DesignVersionHistory DesignVersionHistory { get; set; } = null!;

}
