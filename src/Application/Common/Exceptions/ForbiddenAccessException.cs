using sp26se058_3dprintshop_be.Application.Common.Constants;

namespace sp26se058_3dprintshop_be.Application.Common.Exceptions;

public class ForbiddenAccessException : BusinessException
{
    public ForbiddenAccessException(string message = "Bạn không có quyền thực hiện hành động này.")
        : base(message, ResponseCodeConstants.FORBIDDEN) { }
    
    // Hàm tiện ích để ghi đè thông báo nhanh
    public static ForbiddenAccessException OnlyForCustomer()
        => new ForbiddenAccessException("Chỉ có khách hàng (Customer) mới có quyền sử dụng chức năng này.");

    public static ForbiddenAccessException OnlyForStaff()
        => new ForbiddenAccessException("Chức năng này dành riêng cho nhân viên điều phối.");
}
