using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Materials.Queries;

public class MaterialDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public decimal BaseCostPerGram { get; set; }
    public decimal TotalServiceCostPerGram { get; set; }
    public DateTime EffectiveDate { get; set; }
    public List<MaterialPriceHistoryDTO> PriceHistories { get; set; } = new List<MaterialPriceHistoryDTO>();
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.Material, MaterialDTO>()
    .ForMember(dest => dest.BaseCostPerGram,
        opt => opt.MapFrom(src => src.PriceHistories.FirstOrDefault(p => p.IsCurrent)!.BaseCostPerGram))
    .ForMember(dest => dest.TotalServiceCostPerGram,
        opt => opt.MapFrom(src => src.PriceHistories.FirstOrDefault(p => p.IsCurrent)!.TotalServiceCostPerGram))
    .ForMember(dest => dest.EffectiveDate,
        opt => opt.MapFrom(src => src.PriceHistories.FirstOrDefault(p => p.IsCurrent)!.EffectiveDate))
    .ForMember(dest => dest.PriceHistories,
        opt => opt.MapFrom(src => src.PriceHistories.Where(p => p.IsCurrent)));
        }
    }
}

public class MaterialPriceHistoryDTO
{
    public Guid Id { get; set; }
    public decimal BaseCostPerGram { get; set; }
    public decimal TotalServiceCostPerGram { get; set; }
    public DateTime EffectiveDate { get; set; }
    public bool IsCurrent { get; set; } = true;
    public DateTimeOffset Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModified { get; set; }

    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? Deleted { get; set; }

    public string? DeletedBy { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.MaterialPriceHistory, MaterialPriceHistoryDTO>()
                .ForMember(dest => dest.BaseCostPerGram,
                    opt => opt.MapFrom(src => src.BaseCostPerGram))
                .ForMember(dest => dest.TotalServiceCostPerGram,
                    opt => opt.MapFrom(src => src.TotalServiceCostPerGram))
                .ForMember(dest => dest.EffectiveDate,
                    opt => opt.MapFrom(src => src.EffectiveDate))
                .ForMember(dest => dest.IsCurrent,
                    opt => opt.MapFrom(src => src.IsCurrent));
        }
    }
}
