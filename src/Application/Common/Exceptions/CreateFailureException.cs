using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;
public class CreateFailureException : BusinessException
{
    public CreateFailureException(string entityName, string reason)
        : base($"Tạo {entityName} thất bại: {reason}", ResponseCodeConstants.FAILED) { }
}
