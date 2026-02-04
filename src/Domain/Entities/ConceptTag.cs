using System.ComponentModel.DataAnnotations;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class ConceptTag : BaseAuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsMainTag { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<DesignTag> DesignTags { get; set; } = new List<DesignTag>();
}
