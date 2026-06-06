using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class ServicePackage : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string ServiceType { get; set; } = null!; // DESIGN | PRINTING
    public decimal BasePrice { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<PackageOption> PackageOptions { get; set; } = new List<PackageOption>();
}
