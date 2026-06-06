using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class Material : BaseAuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Tồn kho nguyên liệu (gram).</summary>
    public decimal StockQuantityGrams { get; set; }

    public virtual ICollection<MaterialPriceHistory> PriceHistories { get; set; } = new List<MaterialPriceHistory>();
    public virtual ICollection<MaterialInventoryTransaction> InventoryTransactions { get; set; } = new List<MaterialInventoryTransaction>();
}
