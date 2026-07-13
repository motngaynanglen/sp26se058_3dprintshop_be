using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ShippingAddresses.Queries;
public class ShippingAddressDTO
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string? ReceiverName { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public bool IsDefault { get; set; } = false;
    public int? GhnDistrictId { get; set; }
    public string? GhnWardCode { get; set; }
    //public Customer Customer { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ShippingAddress, ShippingAddressDTO>();
        }
       
    }
}
