using AutoMapper;
using System;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

public class DesignVariantDTO
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool IsAllowPreOrder { get; set; }
    public string? PreviewModelUrl { get; set; }
    public string? PreviewImageUrl { get; set; }
    public decimal? SizeScale { get; set; }
    public decimal? EstimatedWeightPerUnit { get; set; }
    public decimal? EstimatedPrintTimePerUnit { get; set; }

    public Guid DesignTemplateId { get; set; }
    public string? DesignTemplateName { get; set; }
    public string? DesignTemplateThumbnailUrl { get; set; }
    public string? DesignTemplateFileUrl { get; set; }

    /// <summary>File 3D hiển thị: biến thể override hoặc file mẫu.</summary>
    public string? EffectivePreviewModelUrl { get; set; }
    public string? EffectiveThumbnailUrl { get; set; }
    public bool UsesTemplateMedia { get; set; }

    public Guid MaterialId { get; set; }
    public string? MaterialName { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.DesignVariant, DesignVariantDTO>()
                .ForMember(d => d.DesignTemplateName, opt => opt.MapFrom(s => s.DesignTemplate.Name))
                .ForMember(d => d.DesignTemplateThumbnailUrl,
                    opt => opt.MapFrom(s => s.DesignTemplate.ThumbnailUrl))
                .ForMember(d => d.DesignTemplateFileUrl, opt => opt.MapFrom(s => s.DesignTemplate.FileUrl))
                .ForMember(d => d.EffectivePreviewModelUrl, opt => opt.MapFrom(s =>
                    !string.IsNullOrWhiteSpace(s.PreviewModelUrl)
                        ? s.PreviewModelUrl
                        : s.DesignTemplate.FileUrl))
                .ForMember(d => d.EffectiveThumbnailUrl, opt => opt.MapFrom(s =>
                    !string.IsNullOrWhiteSpace(s.PreviewImageUrl)
                        ? s.PreviewImageUrl
                        : s.DesignTemplate.ThumbnailUrl))
                .ForMember(d => d.UsesTemplateMedia, opt => opt.MapFrom(s =>
                    string.IsNullOrWhiteSpace(s.PreviewModelUrl)
                    && string.IsNullOrWhiteSpace(s.PreviewImageUrl)))
                .ForMember(d => d.MaterialName, opt => opt.MapFrom(s => s.Material.Name));
        }
    }
}
