namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

public record GhnProvinceDto(int ProvinceId, string ProvinceName);

public record GhnDistrictDto(int DistrictId, string DistrictName);

public record GhnWardDto(string WardCode, string WardName);

public interface IGhnMasterDataService
{
    Task<IReadOnlyList<GhnProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GhnDistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GhnWardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default);

    /// <summary>Kiem tra quan/huyen thuoc tinh (cache master data GHN).</summary>
    Task<bool> IsDistrictInProvinceAsync(int districtId, int provinceId, CancellationToken cancellationToken = default);
}
