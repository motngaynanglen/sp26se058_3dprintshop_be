using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class ServiceOption : BaseAuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string OptionType { get; set; } = null!; // ADDON | CONFIG
    public decimal DefaultPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public virtual ICollection<PackageOption> PackageOptions { get; set; } = new List<PackageOption>();

}
