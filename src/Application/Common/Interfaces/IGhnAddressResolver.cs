namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

public record GhnAddressResolveInput(string? Ward, string? District, string? City, string? Province);

public record GhnAddressResolveResult(int DistrictId, string WardCode);

/// <summary>Map ten Phuong/Quan/Tinh -> ma GHN tu master data.</summary>
public interface IGhnAddressResolver
{
    Task<GhnAddressResolveResult?> TryResolveAsync(
        GhnAddressResolveInput input,
        CancellationToken cancellationToken = default);
}
