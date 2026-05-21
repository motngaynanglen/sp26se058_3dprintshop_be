using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.DesignTags.Queries;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;

public class DesignTemplateDTO
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? FileUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string CatalogStatus { get; set; } = null!;
    public bool IsActive { get; set; }
    public List<DesignVariantDTO> Variants { get; set; } = new();
    public List<DesignTagDTO> Tags { get; set; } = new();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DesignTemplate, DesignTemplateDTO>()
                .ForMember(d => d.Tags, opt => opt.MapFrom(s => s.DesignTags.Where(x => x.IsActive && x.ConceptTag.IsActive)));
        }   
    }
}
