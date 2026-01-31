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
    public decimal BasePrice { get; set; } = decimal.Zero;
    public string? Description { get; set; }
    public bool IsSupported { get; set; } = true;
    public string? HtmlRaw { get; set; }

    public virtual ICollection<DesignWork> DesignWorks { get; set; } = new List<DesignWork>();
}
