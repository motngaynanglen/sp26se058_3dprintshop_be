using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class MaterialPriceHistory : BaseAuditableEntity
{
    public Guid MaterialId { get; set; }
    public decimal BaseCostPerGram { get; set; }
    public decimal TotalServiceCostPerGram { get; set; }
    public DateTime EffectiveDate { get; set; }
    public bool IsCurrent { get; set; } = true;

    public virtual Material Material { get; set; } = null!;
}
