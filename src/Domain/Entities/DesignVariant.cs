using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class DesignVariant : BaseAuditableEntity
{
    // All setup database in migration configuration :D
    // Base Data 
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; } 
    public decimal Price { get; set; } = decimal.Zero;
    public string? PreviewModelUrl { get; set; }
    // Category
    public Guid DesignTemplateId { get; set; }
    public Guid MaterialId { get; set; }

    public decimal? SizeScale { get; set; }
    // Stock Inventory
    public int StockQuantity { get; set; } = 0;
    public int? MinimumStockLevel { get; set; }
    public bool IsAllowPreOrder { get; set; } = false;
    //
    public decimal? EstimatedWeightPerUnit { get; set; }
    public decimal? EstimatedPrintTimePerUnit { get; set; } // Đơn vị theo phút
    public decimal MarkupPercentage { get; set; } = 0; //  Phần này là độ khó bản mẫu, mặc định 0 đi (có thể xóa sau này) 
    //
    public bool IsActive { get; set; } = true;

    public virtual DesignTemplate DesignTemplate { get; set; } = null!;
    public virtual Material Material { get; set; } = null!;
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
