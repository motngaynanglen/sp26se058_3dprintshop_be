using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;
public class DataNotFoundException : BusinessException
{
    // Mặc định dùng mã DB_004 nếu không truyền code khác
    public DataNotFoundException(string message = "Mục tiêu không tồn tại.")
        : base(message, ResponseCodeConstants.NOT_EXIST) { }

    // Dùng khi muốn chỉ đích danh đối tượng không tồn tại (Ví dụ: DB_004)
    // Dùng khi tìm ID không thấy (DB_004)
    public DataNotFoundException(string entityName, object key)
        : base($"{entityName} với mã ({key}) không tồn tại trên hệ thống.", ResponseCodeConstants.NOT_EXIST) { }
}
