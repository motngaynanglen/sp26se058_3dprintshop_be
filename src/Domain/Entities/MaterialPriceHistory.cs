using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class MaterialPriceHistory : BaseAuditableEntity
{
    [Required]
    [ForeignKey(nameof(MaterialId))]
    public Guid MaterialId { get; set; }
    public int MinOrderValue { get; set; }
    //[Column(TypeName = "decimal(18,2)")]
    public decimal BaseCostPerGram { get; set; }
    //[Column(TypeName = "decimal(18,2)")]
    public decimal TotalServiceCostPerGram { get; set; }
    public DateTime EffectiveDate { get; set; }
    public bool IsCurrent { get; set; }

    public virtual Material Material { get; set; } = null!;
}
