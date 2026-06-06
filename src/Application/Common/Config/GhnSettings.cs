namespace sp26se058_3dprintshop_be.Application.Common.Config;

public class GhnSettings
{
    public const string SectionName = "GHN";

    public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api";

    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Mã kho GHN gửi header <c>ShopId</c> — lấy field <c>_id</c> từ POST /v2/shop/all (vd. 3DPRINTSHOP = 200382),
    /// <b>không</b> dùng client_id hiển thị trên portal (vd. 2509007).
    /// </summary>
    public int ShopId { get; set; }

    /// <summary>client_id trên 5sao.ghn.dev — chỉ tham chiếu, không gửi API.</summary>
    public int? ClientId { get; set; }

    /// <summary>Kho lấy hàng — district_id GHN.</summary>
    public int FromDistrictId { get; set; }

    public string FromWardCode { get; set; } = string.Empty;

    /// <summary>Địa chỉ / SĐT kho lấy hàng (from_* / return_* khi tạo đơn).</summary>
    public string? FromAddress { get; set; }

    public string? FromPhone { get; set; }

    /// <summary>Tên người / cửa hàng gửi hàng (from_name).</summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Mã bưu cục lấy hàng (station/get). 0 = shipper đến địa chỉ kho (from_*).
    /// Không dùng ShopId ở đây.
    /// </summary>
    public int PickStationId { get; set; }

    /// <summary>Mặc định khi địa chỉ khách chưa map mã GHN.</summary>
    public int? DefaultToDistrictId { get; set; }

    public string? DefaultToWardCode { get; set; }

    /// <summary>Tỉnh/TP của kho lấy hàng (vd. HCM = 202) — dùng phân loại nội thành vs liên tỉnh khi GHN trả phí 0.</summary>
    public int FromProvinceId { get; set; }

    public int DefaultWeightGrams { get; set; } = 500;

    public int ServiceTypeId { get; set; } = 2;

    /// <summary>
    /// Mã dịch vụ GHN (service_id từ available-services). Nếu 0 thì tự tra theo from/to district.
    /// </summary>
    public int ServiceId { get; set; }

    public decimal FallbackFee { get; set; } = 30_000;

    /// <summary>Phí ước tính nội thành (cùng tỉnh với kho) khi GHN trả 0.</summary>
    public decimal FallbackFeeInnerCity { get; set; } = 20_000;

    /// <summary>Phí ước tính liên tỉnh khi GHN trả 0.</summary>
    public decimal FallbackFeeInterProvince { get; set; } = 45_000;

    public int FallbackLeadDays { get; set; } = 3;

    public int FallbackLeadDaysInnerCity { get; set; } = 1;

    public int FallbackLeadDaysInterProvince { get; set; } = 4;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Token)
        && ShopId > 0
        && FromDistrictId > 0
        && !string.IsNullOrWhiteSpace(FromWardCode);
}
