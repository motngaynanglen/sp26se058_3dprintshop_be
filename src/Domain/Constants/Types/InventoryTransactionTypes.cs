using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Constants.Types;
public static class InventoryTransactionTypes
{
    public const string PurchaseIn = "PURCHASE_IN";     // Nhập hàng từ nhà cung cấp
    public const string ProductionIn = "PRODUCTION_IN"; // Nhập kho sau khi in 3D xong
    public const string OrderOut = "ORDER_OUT";         // Xuất kho khi khách mua hàng
    public const string OrderCancelReturn = "CANCEL_RETURN"; // Nhập lại kho do khách hủy đơn
    public const string Adjustment = "ADJUSTMENT";       // Điều chỉnh (kiểm kê, hư hỏng)

    public static readonly List<StatusDefinition> All = new()
    {
        new(PurchaseIn, "Nhập mua", "Nhập thêm nguyên liệu hoặc sản phẩm từ nhà cung cấp."),
        new(ProductionIn, "Nhập sản xuất", "Sản phẩm hoàn thành từ máy in 3D được đưa vào kho."),
        new(OrderOut, "Xuất bán", "Số lượng giảm đi do đơn hàng của khách đã thanh toán."),
        new(OrderCancelReturn, "Hủy đơn trả hàng", "Hoàn lại số lượng vào kho do đơn hàng đã hủy hoặc trả về."),
        new(Adjustment, "Điều chỉnh", "Cân bằng kho do kiểm kê thực tế hoặc hàng lỗi.")
    };
    /// <summary>
    /// Các loại giao dịch bắt buộc phải là số DƯƠNG (Nhập kho)
    /// </summary>
    public static bool IsAlwaysPositive(string type) =>
        type == PurchaseIn || type == ProductionIn || type == OrderCancelReturn;

    /// <summary>
    /// Các loại giao dịch bắt buộc phải là số ÂM (Xuất kho)
    /// </summary>
    public static bool IsAlwaysNegative(string type) =>
        type == OrderOut;
}
