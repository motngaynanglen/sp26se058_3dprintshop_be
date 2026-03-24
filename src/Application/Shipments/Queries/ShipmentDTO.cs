using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Application.ShippingAddress.Queries;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Shipments.Queries;
public class ShipmentDTO
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ShippingAddressId { get; set; }
    public Guid? ShippingMethodId { get; set; }
    public string? FullAddress { get; set; }
    public decimal ShippingFee { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
    public string? ShipmentStatus { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // Navigation Properties
    public OrderDTO? Order { get; set; }
    public ShippingAddressDTO? ShippingAddress { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Shipment, ShipmentDTO>()
            //.ForMember(dest => dest.MethodName, opt => opt.MapFrom(src => src.ShippingMethod.Name))
            .ForMember(dest => dest.FullAddress, opt =>
            {
                opt.Condition(src => src.ShippingAddress != null);
                opt.MapFrom(src => string.Join(", ", new[]
                {
                    src.ShippingAddress!.AddressLine,
                    src.ShippingAddress.Ward,
                    src.ShippingAddress.District,
                    src.ShippingAddress.City
                }.Where(str => !string.IsNullOrWhiteSpace(str))));
            })
            .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src => src.ShippingAddress));
        }  
    }
}
