using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class DesignVariant : BaseAuditableEntity
{
    [Required]
    [ForeignKey(nameof(DesignTemplateId))]
    public Guid DesignTemplateId { get; set; }
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = null!;
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;
    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual DesignTemplate DesignTemplate { get; set; } = null!;
    public virtual ICollection<VariantMaterialOption> MaterialOptions { get; set; } = new List<VariantMaterialOption>();
}
