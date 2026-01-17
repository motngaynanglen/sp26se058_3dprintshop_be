using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class DesignTag : BaseAuditableEntity
{
    // Foreign Keys
    [Required]
    [ForeignKey(nameof(ConceptTagId))]
    public Guid ConceptTagId { get; set; }
    [Required]
    [ForeignKey(nameof(DesignTemplateId))]
    public Guid DesignTemplateId { get; set; }

    // Các thuộc tính mở rộng mà bạn yêu cầu
    public bool IsMainTag { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ConceptTag ConceptTag { get; set; } = null!;
    public virtual DesignTemplate DesignTemplate { get; set; } = null!;
}
