using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Statuses;

public static class DesignWorkStatus
{
    public const string Sketching = "SKETCHING";    // Giai đoạn khách tự dùng AI hoặc đang chuẩn bị yêu cầu (nếu hủy thanh toán, hoặc order cũng về trạng thái này)
    public const string Pending = "PENDING";        // Đã gửi yêu cầu, chờ Staff xác nhận
    public const string InProgress = "IN_PROGRESS";  // Staff đang thực hiện vẽ/sửa theo yêu cầu chuyên sâu
    public const string Reviewing = "REVIEWING";    // Đã có bản vẽ/Technical Draft, chờ khách duyệt giá và chốt in
    public const string Completed = "COMPLETED";    // Đã Approve version cuối - Kết thúc Work


    public static readonly List<StatusDefinition> All = new()
    {
        new(Sketching, "Phác thảo ý tưởng", "Khách hàng đang khởi tạo ý tưởng hoặc sử dụng AI",[Pending]),
        new(Pending, "Đang chờ tiếp nhận", "Đã gửi yêu cầu thiết kế, chờ nhân viên xác nhận",[InProgress, Sketching]),
        new(InProgress, "Đang thực hiện", "Nhân viên đang thiết kế chi tiết",[Reviewing]),
        // Bug fix: added Sketching as a valid next state from Reviewing.
        // Required for: (1) POD all-files-rejected → SKETCHING, (2) CancelOrderCommand → SKETCHING.
        new(Reviewing, "Đang kiểm duyệt", "Đang chờ khách hàng duyệt mẫu hoặc báo giá in",[InProgress, Completed, Sketching]),
        new(Completed, "Đã nghiệm thu", "Bản thiết kế đã được duyệt và đóng lại",[])
    };

}
