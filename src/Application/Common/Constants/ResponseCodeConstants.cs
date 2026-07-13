using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Common.Constants;
public static class ResponseCodeConstants
{
    // Thành công
    public const string SUCCESS = "SUCCESS"; // Thành công chung
    public const string CREATED = "CREATED"; // Tạo mới thành công
    public const string UPDATED = "UPDATED"; // Cập nhật thành công
    public const string DELETED = "DELETED"; // Xóa thành công

    // Lỗi hệ thống
    public const string FAILED = "SYS_000";               // Thất bại chung
    public const string NOT_SUPPORTED = "SYS_001";        // Phương thức/Tính năng chưa hỗ trợ
    public const string INTERNAL_SERVER_ERROR = "SYS_500"; // Lỗi server (500)
    public const string UNAUTHORIZED = "SYS_401";          // Chưa đăng nhập
    public const string FORBIDDEN = "SYS_403";             // Không có quyền truy cập

    // Lỗi kiểm duyệt dữ liệu (VALIDATION - VAL) 
    public const string VAL_GENERAL = "VAL_000";          // Lỗi kiểm duyệt chung (400)
    public const string VAL_INVALID_INPUT = "VAL_001";    // Dữ liệu đầu vào sai quy tắc (Trống, sai định dạng)
    public const string VAL_INVALID_STATE = "VAL_002";    // Thực hiện hành động sai trạng thái nghiệp vụ
    public const string VAL_BUSINESS_RESTRICTION = "VAL_003"; // Vi phạm quy tắc kinh doanh (Hết hạn, quá tải)

    // Lỗi xác thực (Auth)
    public const string INVALID_CREDENTIALS = "AUTH_001";  // Sai tài khoản/mật khẩu
    public const string USER_LOCKED = "AUTH_002";          // Tài khoản bị khóa
    public const string TOKEN_EXPIRED = "AUTH_003";        // Token hết hạn
    public const string TOKEN_INVALID = "AUTH_004";        // Token không hợp lệ

    // Lỗi CRUD cơ sở dữ liệu (DATABASE)
    public const string EMPTY_RESULT = "DB_000";           // Trạng thái 200 - Truy vấn thành công nhưng không có dữ liệu
    public const string DUPLICATE_ERROR = "DB_001";        // Trùng lặp dữ liệu (Unique constraint)
    public const string DELETE_FAILED = "DB_002";          // Không thể xóa (do ràng buộc khóa ngoại...)
    public const string UPDATE_FAILED = "DB_003";          // Không thể cập nhật
    public const string NOT_EXIST = "DB_004";              // Bản ghi không tồn tại trong DB

    // Lỗi bên thứ 3 (EXTERNAL - EXT) 
    public const string EXTERNAL_ERROR = "EXT_500";          // Lỗi kết nối chung từ bên thứ 3
    public const string PAYOS_ERROR = "EXT_501";          // Lỗi kết nối hoặc xử lý từ PayOS
    public const string STORAGE_ERROR = "EXT_502";        // Lỗi dịch vụ lưu trữ (VietNix VPS, Cloud)
}
