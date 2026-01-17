using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class VariantMaterialOption : BaseAuditableEntity
{
    [Required]
    [ForeignKey(nameof(DesignVariantId))]
    public Guid DesignVariantId { get; set; }
    [Required]
    [ForeignKey(nameof(MaterialId))]
    public Guid MaterialId { get; set; }
    //[Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedWeight_Grams { get; set; }
    //[Column(TypeName = "decimal(18,2)")]
    public decimal MarkupPercentage { get; set; } // Phụ thu theo độ khó mẫu
    public bool IsDefault { get; set; }

    public virtual DesignVariant DesignVariant { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
}
