namespace sp26se058_3dprintshop_be.Application.Common.Config;

public class GhnSettings
{
    public const string SectionName = "GHN";

    public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api";

    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Ma kho GHN gui header <c>ShopId</c> — lay field <c>_id</c> tu POST /v2/shop/all (vd. 3DPRINTSHOP = 200382),
    /// <b>khong</b> dung client_id hien thi tren portal (vd. 2509007).
    /// </summary>
    public int ShopId { get; set; }

    /// <summary>client_id tren 5sao.ghn.dev — chi tham chieu, khong gui API.</summary>
    public int? ClientId { get; set; }

    /// <summary>Kho lay hang — district_id GHN.</summary>
    public int FromDistrictId { get; set; }

    public string FromWardCode { get; set; } = string.Empty;

    /// <summary>Dia chi / SDT kho lay hang (from_* / return_* khi tao don).</summary>
    public string? FromAddress { get; set; }

    public string? FromPhone { get; set; }

    /// <summary>Ten nguoi / cua hang gui hang (from_name).</summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Ma buu cuc lay hang (station/get). 0 = shipper den dia chi kho (from_*).
    /// Khong dung ShopId o day.
    /// </summary>
    public int PickStationId { get; set; }

    /// <summary>Mac dinh khi dia chi khach chua map ma GHN.</summary>
    public int? DefaultToDistrictId { get; set; }

    public string? DefaultToWardCode { get; set; }

    /// <summary>Tinh/TP cua kho lay hang (vd. HCM = 202) — dung phan loai noi thanh vs lien tinh khi GHN tra phi 0.</summary>
    public int FromProvinceId { get; set; }

    public int DefaultWeightGrams { get; set; } = 500;

    public int ServiceTypeId { get; set; } = 2;

    /// <summary>
    /// Ma dich vu GHN (service_id tu available-services). Neu 0 thi tu tra theo from/to district.
    /// </summary>
    public int ServiceId { get; set; }

    public decimal FallbackFee { get; set; } = 30_000;

    /// <summary>Phi uoc tinh noi thanh (cung tinh voi kho) khi GHN tra 0.</summary>
    public decimal FallbackFeeInnerCity { get; set; } = 20_000;

    /// <summary>Phi uoc tinh lien tinh khi GHN tra 0.</summary>
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
