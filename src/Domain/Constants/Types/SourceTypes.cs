using System.Collections.Generic;
using System.Linq;

namespace sp26se058_3dprintshop_be.Domain.Constants.Types;

/// <summary>
/// Phân loại nguồn (SourceType) của <c>DesignWork</c> và <c>OrderItem</c>.
/// Quy ước chuỗi tương thích ngược với code đã có (vd. <c>"ORDER"</c> trong CancelOrderCommand).
/// </summary>
public static class SourceTypes
{
    /// <summary>Sản phẩm có sẵn trong kho (mua nhanh từ Design Templates).</summary>
    public const string InStock = "IN_STOCK";

    /// <summary>Sản phẩm cho đặt trước (chưa đủ kho, sẽ in thêm).</summary>
    public const string PreOrder = "PRE_ORDER";

    /// <summary>Đặt in từ file 3D do khách upload (Luồng 2 - STL/OBJ + chat báo giá).</summary>
    public const string CustomFilePrintMainflow2 = "CUSTOM_FILE_PRINT_MF2";

    /// <summary>Báo giá theo mô tả/ảnh ý tưởng (Mainflow 2 cũ).</summary>
    public const string CustomQuoteMainflow2 = "CUSTOM_QUOTE_MF2";

    /// <summary>Mô hình do AI sinh ra (Luồng 3 - Image-to-3D).</summary>
    public const string AiGenerated = "AI_GENERATED";

    /// <summary>In từ thiết kế đã hoàn thành trên hệ thống (TH2).</summary>
    public const string PrintFromDesignMainflow2 = "PRINT_FROM_DESIGN_MF2";

    /// <summary>In lại đơn custom đã có báo giá (TH3).</summary>
    public const string ReprintMainflow2 = "REPRINT_MF2";

    /// <summary>Alias tương thích ngược: cùng nghĩa <see cref="InStock"/>. Đã dùng trong CancelOrderCommand.</summary>
    public const string Order = "ORDER";

    public static readonly List<StatusDefinition> All = new()
    {
        new(InStock, "Hàng có sẵn", "#4CAF50", "Sản phẩm 3D đã in sẵn trong kho, giao nhanh."),
        new(PreOrder, "Đặt trước", "#FF9800", "Sản phẩm chưa đủ kho, sẽ được in thêm sau khi đặt."),
        new(CustomFilePrintMainflow2, "In từ file của khách", "#03A9F4", "Khách upload STL/OBJ/GLB, kỹ thuật viên review & báo giá."),
        new(CustomQuoteMainflow2, "Báo giá theo mô tả", "#7E57C2", "Khách gửi mô tả + ảnh ý tưởng, hai bên chat & chốt giá."),
        new(AiGenerated, "Mô hình AI", "#E91E63", "Hệ thống dùng AI sinh model 3D từ ảnh khách upload."),
        new(PrintFromDesignMainflow2, "In từ thiết kế có sẵn", "#00897B", "Khách chọn thiết kế đã thanh toán trên hệ thống để in."),
        new(ReprintMainflow2, "In lại", "#795548", "Khách in lại đơn custom đã có báo giá."),
    };

    public static bool IsCustomPrintFlow(string? sourceType) =>
        sourceType is CustomFilePrintMainflow2
            or CustomQuoteMainflow2
            or AiGenerated
            or PrintFromDesignMainflow2
            or ReprintMainflow2;

    /// <summary>Chuỗi liệt kê các SourceType hợp lệ — dùng cho mô tả Swagger.</summary>
    public static readonly string ListString = string.Join(" / ", All.Select(s => s.Value));
}
