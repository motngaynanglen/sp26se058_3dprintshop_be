using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class DesignTemplate : BaseAuditableEntity
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = null!;
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    [Required]
    public string File_Url { get; set; } = null!;
    public string? Thumbnail_Url { get; set; }

    public virtual ICollection<DesignVariant> Variants { get; set; } = new List<DesignVariant>();
    public virtual ICollection<DesignTag> DesignTags { get; set; } = new List<DesignTag>();
}
