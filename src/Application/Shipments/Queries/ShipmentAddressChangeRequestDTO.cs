using sp26se058_3dprintshop_be.Application.ShippingAddresses.Queries;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Shipments.Queries;

public class ShipmentAddressChangeRequestDTO
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid RequestedByCustomerId { get; set; }
    public Guid NewShippingAddressId { get; set; }
    public Guid? ReviewedByAccountId { get; set; }
    public string Status { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? ResponseNote { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public ShippingAddressDTO? NewShippingAddress { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ShipmentAddressChangeRequest, ShipmentAddressChangeRequestDTO>();
        }
    }
}
