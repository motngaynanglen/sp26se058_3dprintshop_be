using sp26se058_3dprintshop_be.Application.ShippingAddresses.Queries;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Shipments.Queries;

public class ShipmentDTO
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ShippingAddressId { get; set; }
    public decimal ShippingFee { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? CarrierOrderCode { get; set; }
    public string? CarrierStatus { get; set; }
    public string? CarrierLabelUrl { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
    public string ShipmentStatus { get; set; } = string.Empty;
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTimeOffset Created { get; set; }
    public string? FullAddress { get; set; }
    public ShippingAddressDTO? ShippingAddress { get; set; }

    private static string? FormatFullAddress(ShippingAddress? address)
    {
        if (address == null) return null;
        var parts = new[] { address.AddressLine, address.Ward, address.District, address.City }
            .Where(str => !string.IsNullOrWhiteSpace(str));
        return string.Join(", ", parts);
    }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Shipment, ShipmentDTO>()
                .ForMember(dest => dest.FullAddress, opt => opt.Ignore())
                .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src => src.ShippingAddress))
                .AfterMap((src, dest) => dest.FullAddress = FormatFullAddress(src.ShippingAddress));
        }
    }
}
