using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class OrderItem : BaseAuditableEntity
{
    [Required]
    public Guid OrderId { get; set; }
    [ForeignKey(nameof(OrderId))]
    public virtual Order Order { get; set; } = null!;

    public Guid? DesignVariantId { get; set; }
    [ForeignKey(nameof(DesignVariantId))]
    public virtual DesignVariant? DesignVariant { get; set; }

    [Required]
    public Guid MaterialId { get; set; }
    [ForeignKey(nameof(MaterialId))]
    public virtual Material Material { get; set; } = null!;

    public int QuantityOrdered { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
}
