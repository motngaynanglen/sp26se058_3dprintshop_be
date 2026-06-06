using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class PackageOption : BaseAuditableEntity
{
    public Guid ServicePackageId { get; set; }
    public Guid ServiceOptionId { get; set; }

    public bool IsRequired { get; set; }
    public bool DefaultSelected { get; set; }
    public decimal? PriceOverride { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public virtual ServicePackage ServicePackage { get; set; } = null!;
    public virtual ServiceOption ServiceOption { get; set; } = null!;

}
