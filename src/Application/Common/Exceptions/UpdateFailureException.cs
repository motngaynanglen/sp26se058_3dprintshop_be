using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;
public class UpdateFailureException : BusinessException
{
    public UpdateFailureException(string message)
        : base(message, ResponseCodeConstants.UPDATE_FAILED) { }

    public UpdateFailureException(string entityName, string reason)
        : base($"Không thể cập nhật {entityName}. Lý do: {reason}", ResponseCodeConstants.UPDATE_FAILED) { }
}
