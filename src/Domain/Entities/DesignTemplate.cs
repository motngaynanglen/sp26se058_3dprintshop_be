using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class DesignTemplate : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string FileUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    // CatalogStatus đã xóa — chỉ DesignVariant mới có.
    // IsActive giữ lại cho template (soft toggle hiển thị).
    public bool IsActive { get; set; } = true;
    //Navigations
    public virtual ICollection<DesignVariant> Variants { get; set; } = new List<DesignVariant>();
    public virtual ICollection<DesignTag> DesignTags { get; set; } = new List<DesignTag>();
}
