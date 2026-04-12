using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;
public class BusinessException : Exception
{
    public string Code { get; }
    public object? Details { get; }

    public BusinessException(string message, string code = ResponseCodeConstants.FAILED, object? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }
}
