using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.ServiceSelections.Queries;

namespace sp26se058_3dprintshop_be.Application.ServiceSelectionOptions.Queries;
public class ServiceSelectionOptionDTO
{
    public Guid Id { get; set; }
    public Guid ServiceOptionId { get; set; }
    public Guid ServiceSelectionId { get; set; }
    public int Quantity { get; set; }
    public decimal AppliedPrice { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ServiceSelectionOption, ServiceSelectionOptionDTO>();
        }
    }

}
