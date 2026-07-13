using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class ServiceSelectedOption : BaseAuditableEntity
{
    public Guid ServiceSelectionId { get; set; }
    public Guid ServiceOptionId { get; set; }

    public string OptionCodeSnapshot { get; set; } = null!;
    public string OptionNameSnapshot { get; set; } = null!;
    public string? OptionDescriptionSnapshot { get; set; }
    public string? OptionGroupCodeSnapshot { get; set; }
    public string? OptionGroupNameSnapshot { get; set; }
    public int Quantity { get; set; }
    public decimal AppliedPrice { get; set; } // Snapshot giá tại thời điểm chọn
    public int? AdjustmentRoundDeltaSnapshot { get; set; }
    public virtual ServiceSelection ServiceSelection { get; set; } = null!;
    public virtual ServiceOption ServiceOption { get; set; } = null!;

}
