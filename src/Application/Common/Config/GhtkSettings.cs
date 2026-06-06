namespace sp26se058_3dprintshop_be.Application.Common.Config;

public class GhtkSettings
{
    public const string SectionName = "GHTK";

    public string BaseUrl { get; set; } = "https://services.giaohangtietkiem.vn";

    public string Token { get; set; } = string.Empty;

    public string PartnerCode { get; set; } = "3DPRINTSHOP";

    public string PickName { get; set; } = "3D Print Shop";

    public string PickTel { get; set; } = string.Empty;

    public string PickAddress { get; set; } = string.Empty;

    public string PickProvince { get; set; } = string.Empty;

    public string PickDistrict { get; set; } = string.Empty;

    public string PickWard { get; set; } = string.Empty;

    public int DefaultWeightGrams { get; set; } = 500;

    public decimal FallbackFee { get; set; } = 28_000;

    public int FallbackLeadDays { get; set; } = 3;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Token)
        && !string.IsNullOrWhiteSpace(PickTel)
        && !string.IsNullOrWhiteSpace(PickAddress);
}
