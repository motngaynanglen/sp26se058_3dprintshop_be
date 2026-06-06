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
    public const string Adjustment = "ADJUSTMENT";       // Điều chỉnh (kiểm kê, hư hỏng)
    public const string OrderCancelReturn = "OrderCancelReturn"; // Hoàn kho khi hủy đơn

    public static readonly List<StatusDefinition> All = new()
    {
        new(PurchaseIn, "Nhập mua", "#4CAF50", "Nhập thêm nguyên liệu hoặc sản phẩm từ nhà cung cấp."),
        new(ProductionIn, "Nhập sản xuất", "#2196F3", "Sản phẩm hoàn thành từ máy in 3D được đưa vào kho."),
        new(OrderOut, "Xuất bán", "#FF9800", "Số lượng giảm đi do đơn hàng của khách đã thanh toán."),
        new(Adjustment, "Điều chỉnh", "#9C27B0", "Cân bằng kho do kiểm kê thực tế hoặc hàng lỗi."),
        new(OrderCancelReturn, "Hoàn kho", "#00897B", "Nhập lại kho do đơn hàng bị hủy.")
    };

    public static bool IsInbound(int quantity) => quantity > 0;
    public static bool IsOutbound(int quantity) => quantity < 0;

    public static StatusDefinition? Resolve(string? type) =>
        All.FirstOrDefault(t => t.Value == type);
}
