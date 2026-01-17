using System.ComponentModel.DataAnnotations;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class ConceptTag : BaseAuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    // Relationship: Một ConceptTag có thể gắn cho nhiều DesignTemplate
    public virtual ICollection<DesignTag> DesignTags { get; set; } = new List<DesignTag>();
}
