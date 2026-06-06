using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesManageCatalog;

public class DesignTemplateManageCatalogItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string FileUrl { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset Created { get; set; }
    public int VariantCount { get; set; }
    public int ActiveVariantCount { get; set; }
    public List<string> ConceptTagNames { get; set; } = new();
    public List<DesignVariantDTO> Variants { get; set; } = new();
}
