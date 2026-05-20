using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Types;

public static class ServiceOptionSelectionTypes
{
    public const string Addon = "ADDON";
    public const string Single = "SINGLE";
    public const string Multiple = "MULTIPLE";
    public const string Quantity = "QUANTITY";

    public static readonly List<StatusDefinition> All = new()
    {
        new(Addon, "Tùy chọn bổ sung", "Khách có thể chọn thêm vào gói dịch vụ."),
        new(Single, "Chọn một", "Nhóm tùy chọn chỉ nên chọn một mục."),
        new(Multiple, "Chọn nhiều", "Nhóm tùy chọn có thể chọn nhiều mục."),
        new(Quantity, "Theo số lượng", "Tùy chọn cho phép khách nhập số lượng.")
    };

    public static bool IsValid(string? value)
    {
        return All.Any(x => x.Value == value);
    }
}
