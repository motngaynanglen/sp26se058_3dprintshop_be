using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Types;
public static class SourceTypes
{
    public const string InStock = "IN_STOCK";     // Mẫu từ thư viện shop
    public const string PreOrder = "PRE_ORDER";     // Mẫu từ thư viện shop
    public const string DesignService = "DESIGN_SERVICE"; // Dịch vụ thiết kế riêng
    public const string PrintService = "PRINT_SERVICE";   // Khách tự upload file in

    public static readonly List<StatusDefinition> All = new()
    {
        new(InStock, "Hàng có sẵn", "#607D8B", "Sản phẩm lấy từ danh mục tồn kho của cửa hàng."),
        new(PreOrder, "Hàng đặt trước", "#607D8B", "Sản phẩm lấy từ danh mục mẫu in của cửa hàng."),
        new(DesignService, "Dịch vụ thiết kế", "#673AB7", "Yêu cầu vẽ mẫu hoặc chỉnh sửa 3D."),
        new(PrintService, "File khách gửi", "#FF5722", "Khách hàng cung cấp file định dạng .STL/.OBJ để in.")
    };
    public static readonly string ListString = InStock + ", " + PreOrder + ", " + DesignService + ", " + PrintService;
}
