using System.ComponentModel.DataAnnotations;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class Material : BaseAuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    //[Column(TypeName = "decimal(18,4)")]
    public decimal Density { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<MaterialPriceHistory> PriceHistories { get; set; } = new List<MaterialPriceHistory>();
}
