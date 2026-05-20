using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Shipments;

public static class ShipmentAddressSnapshot
{
    public static void Apply(Shipment shipment, ShippingAddress address)
    {
        shipment.ShippingAddressId = address.Id;
        shipment.RecipientName = address.ReceiverName;
        shipment.RecipientPhone = address.Phone;
        shipment.AddressLine = address.AddressLine;
        shipment.Ward = address.Ward;
        shipment.District = address.District;
        shipment.City = address.City;
        shipment.Province = address.Province;
    }

    public static string BuildFullAddress(Shipment shipment)
    {
        return string.Join(", ", new[]
        {
            shipment.AddressLine,
            shipment.Ward,
            shipment.District,
            shipment.City,
            shipment.Province
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
