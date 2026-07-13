using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Application.ShippingAddresses.Queries;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Shipments.Queries;
public class ShipmentDTO
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? ShippingAddressId { get; set; }
    //public Guid? ShippingMethodId { get; set; }
    public string? FullAddress { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public decimal? ShippingFee { get; set; }
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
    public string? ShipmentStatus { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Note { get; set; }

    // Navigation Properties
    public OrderDTO? Order { get; set; }
    public ShippingAddressDTO? ShippingAddress { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Shipment, ShipmentDTO>()
            //.ForMember(dest => dest.MethodName, opt => opt.MapFrom(src => src.ShippingMethod.Name))
            .ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src =>
                src.AddressLine
                + ", " + src.Ward
                + ", " + src.District
                + ", " + src.City
                + ", " + src.Province));
        }
    }
}
