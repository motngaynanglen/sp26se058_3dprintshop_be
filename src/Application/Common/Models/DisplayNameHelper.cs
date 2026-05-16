namespace sp26se058_3dprintshop_be.Application.Common.Models;

public static class DisplayNameHelper
{
    private static readonly Dictionary<string, string> EntityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Account"] = "tài khoản",
        ["Customer"] = "khách hàng",
        ["Staff"] = "nhân viên",
        ["Manager"] = "quản lý",
        ["Order"] = "đơn hàng",
        ["OrderItem"] = "món hàng",
        ["Transaction"] = "giao dịch",
        ["Shipment"] = "vận đơn",
        ["ShippingAddress"] = "địa chỉ giao hàng",
        ["DesignWork"] = "công việc thiết kế",
        ["DesignVersionHistory"] = "phiên bản file thiết kế",
        ["DesignTemplate"] = "mẫu thiết kế",
        ["DesignTemplates"] = "mẫu thiết kế",
        ["DesignVariant"] = "biến thể thiết kế",
        ["DesignTag"] = "nhãn",
        ["DesignLog"] = "log thiết kế",
        ["TechnicalDraft"] = "bản nháp kỹ thuật",
        ["Material"] = "vật liệu",
        ["ServiceOption"] = "tùy chọn dịch vụ",
        ["Feedback"] = "đánh giá",
        ["ConceptTag"] = "nhãn dán"
    };

    private static readonly Dictionary<string, string> FieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Id"] = "mã định danh",
        ["Username"] = "tên đăng nhập",
        ["Email"] = "email",
        ["Code"] = "mã code",
        ["Name"] = "tên",
        ["Role"] = "vai trò",
        ["Phone"] = "số điện thoại",
        ["ContactPhone"] = "số điện thoại"
    };

    public static string Entity(string name)
    {
        return EntityNames.TryGetValue(name, out var displayName) ? displayName : name;
    }

    public static string Field(string name)
    {
        return FieldNames.TryGetValue(name, out var displayName) ? displayName : name;
    }
}
