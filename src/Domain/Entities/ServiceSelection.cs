using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class ServiceSelection : BaseAuditableEntity
{
    public Guid DesignWorkId { get; set; }

    public decimal TotalPrice { get; set; }
    public string? Note { get; set; }
    public bool IsLocked { get; set; }
    public virtual DesignWork DesignWork { get; set; } = null!;
    public virtual ICollection<ServiceSelectedOption> ServiceSelectedOptions { get; set; } = new List<ServiceSelectedOption>();
}
