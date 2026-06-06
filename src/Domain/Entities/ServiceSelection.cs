using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class ServiceSelection : BaseAuditableEntity
{
    public Guid DesignWorkId { get; set; }

    public Guid? ServicePackageId { get; set; }

    public string SelectionType { get; set; } = null!; // FREE_AI | PAID
    public bool IsLocked { get; set; }
    public virtual DesignWork DesignWork { get; set; } = null!;
    public virtual ServicePackage? ServicePackage { get; set; }
    public virtual ICollection<ServiceSelectionOption> SelectedOptions { get; set; } = new List<ServiceSelectionOption>();
}
