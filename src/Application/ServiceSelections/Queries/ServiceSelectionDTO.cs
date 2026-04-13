using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using sp26se058_3dprintshop_be.Application.ServiceSelectionOptions.Queries;

namespace sp26se058_3dprintshop_be.Application.ServiceSelections.Queries;
public class ServiceSelectionDTO
{
    public Guid Id { get; set; }
    public Guid DesignWorkId { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? Note { get; set; }
    public bool? IsLocked { get; set; } 
    public DateTimeOffset? Created { get; set; }
    public ICollection<ServiceSelectedOptionDTO>? ServiceSelectedOptions { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ServiceSelection, ServiceSelectionDTO>();
        }
    }

}
