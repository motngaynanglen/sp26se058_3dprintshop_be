using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Types;
public static class DesignLogType
{
    public const string Communication = "COMMUNICATION"; // Chat giữa khách và nhân viên [cite: 37]
    public const string InternalNote = "INTERNAL_NOTE";   // Ghi chú nội bộ giữa các nhân viên [cite: 37]
    public const string VersionUpdate = "VERSION_UPDATE"; // Thông báo có file 3D/phiên bản mới [cite: 16, 37]
    public const string AiGen = "AI_GEN";                // Lịch sử tương tác với AI [cite: 11, 37]
    public const string StatusChange = "STATUS_CHANGE";   // Nhật ký thay đổi trạng thái dự án [cite: 37]
    public const string System = "SYSTEM";                // Thông báo tự động (thanh toán, báo giá) [cite: 14, 19, 37]
    public const string AdjustmentRequest = "ADJUSTMENT_REQUEST";

    public static readonly List<StatusDefinition> All = new()
    {
        new(Communication, "Trao đổi khách hàng", "Hiển thị dạng bong bóng chat trái/phải."),
        new(InternalNote, "Ghi chú nội bộ", "Chỉ nhân viên nhìn thấy để ghi chú kỹ thuật."),
        new(VersionUpdate, "Cập nhật phiên bản", "Thông báo có file 3D mới kèm nút Preview/Download."),
        new(AiGen, "Lịch sử AI", "Lưu lại các câu lệnh và ảnh mẫu do AI tạo."),
        new(StatusChange, "Thay đổi trạng thái", "Nhật ký hệ thống ghi lại bước tiến dự án."),
        new(System, "Thông báo hệ thống", "Các tin nhắn tự động như xác nhận thanh toán."),
        new(AdjustmentRequest, "Yêu cầu hiệu chỉnh", "Khách yêu cầu hiệu chỉnh trong cùng công việc thiết kế.")
    };
}
