using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.DesignTags.Queries;

public class DesignTagDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsMainTag { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.DesignTag, DesignTagDTO>()
                .ForMember(t => t.Name, opt => opt.MapFrom(m => m.ConceptTag != null ? m.ConceptTag.Name : string.Empty));
        }
    }
    public static void Register(IMapperConfigurationExpression cfg)
    {
        cfg.AddProfile<Mapping>();
    }
}
