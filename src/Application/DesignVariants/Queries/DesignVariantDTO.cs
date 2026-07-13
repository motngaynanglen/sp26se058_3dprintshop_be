using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

public class DesignVariantDTO
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string? PreviewModelUrl { get; set; }
    public Guid DesignTemplateId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal? SizeScale { get; set; }
    public int StockQuantity { get; set; }
    public int? MinimumStockLevel { get; set; }
    public bool IsAllowPreOrder { get; set; } = false;
    public decimal? EstimatedWeightPerUnit { get; set; }
    public decimal? EstimatedPrintTimePerUnit { get; set; }
    public decimal MarkupPercentage { get; set; } = 0;
    public string CatalogStatus { get; set; } = null!;
    public bool IsActive { get; set; }

    // Thông tin join — giúp FE hiển thị đúng mà không cần query thêm
    public string? MaterialName { get; set; }
    public string? DesignTemplateName { get; set; }
    public string? DesignTemplateFileUrl { get; set; }
    public string? DesignTemplateThumbnailUrl { get; set; }

    /// <summary>
    /// Model hiệu lực: variant.PreviewModelUrl nếu có, nếu không → template.FileUrl
    /// </summary>
    public string? EffectivePreviewModelUrl { get; set; }
    /// <summary>
    /// Thumbnail kế thừa từ template
    /// </summary>
    public string? EffectiveThumbnailUrl { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.DesignVariant, DesignVariantDTO>()
                .ForMember(d => d.ImageUrls, opt => opt.MapFrom(s =>
                    !string.IsNullOrEmpty(s.ImageUrls)
                        ? TryDeserializeList(s.ImageUrls)
                        : new List<string>()))
                .ForMember(d => d.MaterialName, opt => opt.MapFrom(s =>
                    s.Material != null ? s.Material.Name : null))
                .ForMember(d => d.DesignTemplateName, opt => opt.MapFrom(s =>
                    s.DesignTemplate != null ? s.DesignTemplate.Name : null))
                .ForMember(d => d.DesignTemplateFileUrl, opt => opt.MapFrom(s =>
                    s.DesignTemplate != null ? s.DesignTemplate.FileUrl : null))
                .ForMember(d => d.DesignTemplateThumbnailUrl, opt => opt.MapFrom(s =>
                    s.DesignTemplate != null ? s.DesignTemplate.ThumbnailUrl : null))
                .ForMember(d => d.EffectivePreviewModelUrl, opt => opt.MapFrom(s =>
                    !string.IsNullOrEmpty(s.PreviewModelUrl)
                        ? s.PreviewModelUrl
                        : (s.DesignTemplate != null ? s.DesignTemplate.FileUrl : null)))
                .ForMember(d => d.EffectiveThumbnailUrl, opt => opt.MapFrom(s =>
                    s.DesignTemplate != null ? s.DesignTemplate.ThumbnailUrl : null));
        }

        private static List<string> TryDeserializeList(string json)
        {
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
            catch { return new List<string>(); }
        }
        
    }
    
}
