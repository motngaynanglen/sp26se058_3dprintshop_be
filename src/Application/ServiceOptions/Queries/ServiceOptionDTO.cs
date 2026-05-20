using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Queries;

public class ServiceOptionDTO
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? GroupCode { get; set; }
    public string? GroupName { get; set; }
    public string? SelectionType { get; set; }
    public decimal? DefaultPrice { get; set; }
    public int MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public int? AdjustmentRoundDelta { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ServiceOption, ServiceOptionDTO>();
        }
    }
}
