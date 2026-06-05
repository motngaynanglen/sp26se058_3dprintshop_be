using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class ShippingCarrierResolver : IShippingCarrierResolver
{
    private readonly IReadOnlyDictionary<string, IShippingCarrierService> _byCode;

    public ShippingCarrierResolver(IEnumerable<IShippingCarrierService> services)
    {
        _byCode = services.ToDictionary(
            s => s.CarrierCode,
            s => s,
            StringComparer.OrdinalIgnoreCase);
    }

    public IShippingCarrierService? GetService(string carrierCode) =>
        string.IsNullOrWhiteSpace(carrierCode)
            ? null
            : _byCode.GetValueOrDefault(carrierCode.Trim());

    public IReadOnlyList<IShippingCarrierService> GetAll() => _byCode.Values.ToList();
}
