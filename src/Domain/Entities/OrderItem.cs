using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class OrderItem : BaseAuditableEntity
{
    [Required]
    public Guid OrderId { get; set; }


    // phân loại hàng hóa
    public required string SourceType { get; set; }
    public Guid? DesignVariantId { get; set; }

    public Guid? ServiceSelectionId { get; set; }
    public Guid? DesignWorkId { get; set; }
    public Guid? TechnicalDraftId { get; set; }
    public string? ItemName { get; set; }

    public required int QuantityOrdered { get; set; }
    public required decimal UnitPrice { get; set; }
    public required decimal TotalPrice { get; set; }
    public required string FulfillmentStatus { get; set; }

    // Snapshot giá tại thời điểm chốt
    //public decimal TotalServiceCostPerGram { get; set; } = 0; // cái này tạm bỏ đi nhỉ? khách cần gì biết 1 gram bao tiền đâu!

    // Nativigation
    public required virtual Order Order { get; set; }
    public virtual DesignWork? DesignWork { get; set; }
    public virtual ServiceSelection? ServiceSelection { get; set; }
    public virtual DesignVariant? DesignVariant { get; set; }
    public virtual TechnicalDraft? TechnicalDraft { get; set; }
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
