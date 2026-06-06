namespace sp26se058_3dprintshop_be.Domain.Constants.Types;

/// <summary>Đơn vị vận chuyển bên thứ ba.</summary>
public static class ShippingCarriers
{
    public const string Manual = "MANUAL";
    public const string Ghn = "GHN";
    public const string Ghtk = "GHTK";

    public static readonly IReadOnlySet<string> ThirdParty = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Ghn,
        Ghtk
    };

    public static bool IsThirdParty(string? carrier) =>
        !string.IsNullOrWhiteSpace(carrier) && ThirdParty.Contains(carrier.Trim());
}
