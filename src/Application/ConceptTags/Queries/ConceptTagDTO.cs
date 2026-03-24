using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Orders.Queries;

namespace sp26se058_3dprintshop_be.Application.ConceptTags.Queries;

public class ConceptTagDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool IsActive { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.ConceptTag, ConceptTagDTO>();
            
        }

    }
}
