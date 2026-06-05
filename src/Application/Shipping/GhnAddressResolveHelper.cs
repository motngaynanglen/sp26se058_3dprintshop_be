using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Shipping;

public static class GhnAddressResolveHelper
{
    public static async Task EnsureGhnCodesAsync(
        ShippingAddress address,
        IGhnAddressResolver resolver,
        CancellationToken cancellationToken = default)
    {
        if (address.GhnDistrictId is > 0 && !string.IsNullOrWhiteSpace(address.GhnWardCode))
            return;

        var result = await resolver.TryResolveAsync(
            new GhnAddressResolveInput(address.Ward, address.District, address.City, address.Province),
            cancellationToken);

        if (result == null)
            return;

        address.GhnDistrictId = result.DistrictId;
        address.GhnWardCode = result.WardCode;
    }
}
