using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class ServiceSelectionOption : BaseAuditableEntity
{
    public Guid ServiceSelectionId { get; set; }
    public Guid ServiceOptionId { get; set; }

    public int Quantity { get; set; }
    public decimal AppliedPrice { get; set; } // Snapshot giá tại thời điểm chọn
    public virtual ServiceSelection ServiceSelection { get; set; } = null!;
    public virtual ServiceOption ServiceOption { get; set; } = null!;

}
