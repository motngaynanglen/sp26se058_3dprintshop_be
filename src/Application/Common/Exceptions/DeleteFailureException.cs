using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;
public class DeleteFailureException : BusinessException
{
    public DeleteFailureException(string message)
        : base(message, ResponseCodeConstants.DELETE_FAILED) { }

    public DeleteFailureException(string entityName, string reason)
        : base($"Xóa {DisplayNameHelper.Entity(entityName)} thất bại. {reason}", ResponseCodeConstants.DELETE_FAILED) { }
}
