using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;
public class DuplicateException : BaseValidationException
{
    public DuplicateException(string message)
        : base(message)
    {
        Errors.Add("General", new[] { message });
    }

    //public DuplicateException(string entityName, string fieldName, object value)
    //    : base($"{entityName} với {fieldName} '{value}' đã tồn tại.")
    //{
    //    Errors = new Dictionary<string, string[]>
    //    {
    //        { fieldName, new[] { $"{entityName} với {fieldName} '{value}' đã tồn tại." } }
    //    };
    //}
    public DuplicateException(string entityName, string fieldName, object value, bool isDeleted = false)
        : base(isDeleted
            ? $"{entityName} với {fieldName} '{value}' đã tồn tại trong thùng rác (đã xóa mềm)."
            : $"{entityName} với {fieldName} '{value}' đã tồn tại."
         )
    {
        var message = isDeleted
            ? $"{entityName} này đã tồn tại trong danh sách đã xóa. Vui lòng khôi phục hoặc dùng {fieldName} khác."
            : $"{entityName} với {fieldName} '{value}' đã tồn tại.";

        Errors.Add(fieldName, new[] { message });
    }
    public DuplicateException(IDictionary<string, string[]> errors)
        : base("Dữ liệu bị trùng lặp.", errors)
    {
    }
}
