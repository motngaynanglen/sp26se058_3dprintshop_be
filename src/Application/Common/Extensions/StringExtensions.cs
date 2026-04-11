using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Extensions;
public static class StringExtensions
{
    /// <summary>
    /// Chuyển đổi string sang Guid, nếu lỗi trả về Guid.Empty (0000...)
    /// </summary>
    public static Guid ToGuid(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Guid.Empty;
        }
        return Guid.TryParse(value, out var result) ? result : Guid.Empty;
    }
    public static long ToLong(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }
        return long.TryParse(value, out var result) ? result : 0;
    }
}
