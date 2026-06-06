namespace sp26se058_3dprintshop_be.Domain.Constants.Types;

public static class MaterialInventoryTransactionTypes
{
    public const string PurchaseIn = "PURCHASE_IN";
    public const string OrderOut = "ORDER_OUT";
    public const string Adjustment = "ADJUSTMENT";
    public const string OrderCancelReturn = "ORDER_CANCEL_RETURN";

    public static readonly List<StatusDefinition> All =
    [
        new(PurchaseIn, "Nhập kho", "#4CAF50", "Nhập bổ sung nguyên liệu (gram)."),
        new(OrderOut, "Xuất in", "#FF9800", "Trừ kho khi đơn Pre-order / In custom vào sản xuất."),
        new(Adjustment, "Điều chỉnh", "#9C27B0", "Kiểm kê hoặc điều chỉnh tồn kho."),
        new(OrderCancelReturn, "Hoàn kho", "#00897B", "Hoàn nguyên liệu khi hủy đơn đã xuất."),
    ];

    public static StatusDefinition? Resolve(string? type) =>
        All.FirstOrDefault(t => t.Value == type);
}
