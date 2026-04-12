using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;
public class DuplicateException : BusinessException
{
    public DuplicateException(string message)
        : base(message, ResponseCodeConstants.DUPLICATE_ERROR) { }

    public DuplicateException(string entityName, string fieldName, object value)
        : base($"{entityName} với {fieldName} '{value}' đã tồn tại.", ResponseCodeConstants.DUPLICATE_ERROR) { }
}
