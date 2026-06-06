namespace sp26se058_3dprintshop_be.Application.Mainflow2.Models;

/// <summary>Input NV gửi khi báo giá — 1 sản phẩm gồm nhiều thành phần, mỗi thành phần 1+ vật liệu + định lượng (gram).</summary>
public record QuoteMaterialLineInput
{
    public Guid MaterialId { get; init; }
    /// <summary>Gram vật liệu cho <b>một</b> đơn vị thành phần.</summary>
    public decimal Grams { get; init; }
}

public record QuoteComponentInput
{
    public required string Name { get; init; }
    public int Quantity { get; init; } = 1;
    public IReadOnlyList<QuoteMaterialLineInput> Materials { get; init; } = Array.Empty<QuoteMaterialLineInput>();
}

/// <summary>Bảng giá chi tiết lưu trong metadata DesignLog (hiển thị khách/NV).</summary>
public class Mainflow2QuoteBreakdownDto
{
    public IReadOnlyList<Mainflow2QuoteComponentLineDto> Components { get; set; } = Array.Empty<Mainflow2QuoteComponentLineDto>();
    public decimal MaterialSubtotal { get; set; }
    /// <summary>Tiền thiết kế (cọc 30% trên khoản này).</summary>
    public decimal LaborCost { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "VND";
}

public class Mainflow2QuoteComponentLineDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public IReadOnlyList<Mainflow2QuoteMaterialLineDto> Materials { get; set; } = Array.Empty<Mainflow2QuoteMaterialLineDto>();
    public decimal LineTotal { get; set; }
}

public class Mainflow2QuoteMaterialLineDto
{
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public decimal GramsPerUnit { get; set; }
    public int ComponentQuantity { get; set; }
    public decimal TotalGrams { get; set; }
    public decimal PricePerGram { get; set; }
    public decimal LineTotal { get; set; }
}
