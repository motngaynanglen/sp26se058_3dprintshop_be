namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;

public static class CatalogStatuses
{
    public const string Draft = "DRAFT";
    public const string Published = "PUBLISHED";
    public const string Archived = "ARCHIVED";

    public static readonly List<StatusDefinition> All =
    [
        new(Draft, "Bản nháp", "Sản phẩm đang được nhập liệu, chưa hiển thị cho khách hàng.", [Published, Archived]),
        new(Published, "Đang bán", "Sản phẩm đã đủ dữ liệu và đang hiển thị trong catalog.", [Draft, Archived]),
        new(Archived, "Đã lưu trữ", "Sản phẩm đã ngừng bán hoặc không còn dùng trong catalog.", [Draft])
    ];

    public static bool IsValid(string? status)
        => All.Any(x => x.Value == status?.ToUpperInvariant());
}
