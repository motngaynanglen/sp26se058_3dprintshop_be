using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class InventoryTransaction : BaseAuditableEntity
{
    public Guid DesignVariantId { get; set; }

    public Guid? StaffId { get; set; } // cho việc chủ động thêm hàng hoặc gì đó

    public required string Type { get; set; } // 'PurchaseIn', 'ProductionIn', 'OrderOut', 'Adjustment'
    public int Quantity { get; set; }
    // Id tham chiếu tới OrderItem hoặc các bảng liên quan (nếu có) (nếu được mua 'PurchaseIn')
    public Guid? ReferenceId { get; set; }

    public string? Note { get; set; }
    public virtual Staff? Staff { get; set; }
    public virtual DesignVariant DesignVariant { get; set; } = null!;

}
