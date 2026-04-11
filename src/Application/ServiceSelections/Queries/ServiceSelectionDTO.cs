using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;

namespace sp26se058_3dprintshop_be.Application.ServiceSelections.Queries;
public class ServiceSelectionDTO
{
    public Guid Id { get; set; }
    public Guid DesignWorkId { get; set; }
    public Guid ServicePackageId { get; set; }

    public string? SelectionType { get; set; }
    public bool? IsLocked { get; set; } 
    public DateTimeOffset? CreatedAt { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ServiceSelection, ServiceSelectionDTO>();
        }
    }

}
