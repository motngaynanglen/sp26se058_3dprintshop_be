using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Models;
public abstract class PaginationRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 10;
    private const int MaxPageSize = 100; // Giới hạn để bảo vệ hệ thống
    [DefaultValue(1)]
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value; // Nếu < 1 thì lấy 1
    }
    [DefaultValue(10)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
    }
}
