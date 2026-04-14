using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Models;
public class DuplicateCheckResult
{
    public bool IsDuplicate { get; set; }
    public bool IsDeleted { get; set; }
    public string EntityName { get; set; } = null!;
    public string FieldName { get; set; } = null!;
    public object Value { get; set; } = null!;

    public void ThrowIfDuplicate()
    {
        if (IsDuplicate)
        {
            throw new DuplicateException(EntityName, FieldName, Value, IsDeleted);
        }
    }
    public string GetErrorMessage()
    {
        return IsDeleted
            ? $"{EntityName} này đã tồn tại trong danh sách đã xóa. Vui lòng khôi phục hoặc dùng {FieldName} khác."
            : $"{EntityName} với {FieldName} '{Value}' đã tồn tại.";
    }
}
