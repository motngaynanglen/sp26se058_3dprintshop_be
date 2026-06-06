namespace sp26se058_3dprintshop_be.Domain.Entities;

public class MaterialInventoryTransaction : BaseAuditableEntity
{
    public Guid MaterialId { get; set; }
    public Guid? StaffId { get; set; }

    public required string Type { get; set; }
    public decimal QuantityGrams { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Note { get; set; }

    public virtual Material Material { get; set; } = null!;
    public virtual Staff? Staff { get; set; }
}
