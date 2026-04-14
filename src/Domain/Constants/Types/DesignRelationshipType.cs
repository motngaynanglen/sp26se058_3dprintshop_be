using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Types;
public static class DesignRelationshipType
{
    public const string Original = "ORIGINAL"; // Không có cha (Root)
    public const string Revision = "REVISION"; // Sửa lỗi/thay đổi nhỏ (Cùng một luồng)
    public const string Branch = "BRANCH";     // Phát triển hướng mới từ mẫu cũ
    public const string Clone = "CLONE";       // Nhân bản từ một Template mẫu

    public static readonly List<StatusDefinition> All = new()
    {
        new(Original, "Bản gốc", "Bản thiết kế đầu tiên của dự án."),
        new(Revision, "Bản chỉnh sửa", "Chỉnh sửa trực tiếp trên luồng thiết kế hiện tại."),
        new(Branch, "Nhánh mới", "Tạo một hướng đi khác dựa trên bản thiết kế cũ."),
        new(Clone, "Sao chép", "Tạo mới dựa trên một mẫu thiết kế có sẵn.")
    };
}
