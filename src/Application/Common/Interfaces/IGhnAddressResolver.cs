namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

public record GhnAddressResolveInput(string? Ward, string? District, string? City, string? Province);

public record GhnAddressResolveResult(int DistrictId, string WardCode);

/// <summary>Map tên Phường/Quận/Tỉnh → mã GHN từ master data.</summary>
public interface IGhnAddressResolver
{
    Task<GhnAddressResolveResult?> TryResolveAsync(
        GhnAddressResolveInput input,
        CancellationToken cancellationToken = default);
}
