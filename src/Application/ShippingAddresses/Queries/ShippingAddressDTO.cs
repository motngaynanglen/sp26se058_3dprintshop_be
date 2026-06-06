using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ShippingAddresses.Queries;

public class ShippingAddressDTO
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string ReceiverName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string AddressLine { get; set; } = null!;
    public string Ward { get; set; } = null!;
    public string District { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Province { get; set; } = null!;
    public int? GhnDistrictId { get; set; }
    public string? GhnWardCode { get; set; }
    public bool IsDefault { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ShippingAddress, ShippingAddressDTO>();
        }
    }
}
