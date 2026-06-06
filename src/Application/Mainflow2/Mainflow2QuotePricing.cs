using sp26se058_3dprintshop_be.Application.Mainflow2.Models;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Mainflow2;

public static class Mainflow2QuotePricing
{
    public static Mainflow2QuoteBreakdownDto BuildBreakdown(
        IReadOnlyList<QuoteComponentInput> components,
        decimal laborCost,
        IReadOnlyDictionary<Guid, Material> materialsById,
        string currency = "VND")
    {
        var componentLines = new List<Mainflow2QuoteComponentLineDto>();
        decimal materialSubtotal = 0;

        foreach (var comp in components)
        {
            var name = comp.Name?.Trim() ?? "Thành phần";
            var qty = comp.Quantity < 1 ? 1 : comp.Quantity;
            var materialLines = new List<Mainflow2QuoteMaterialLineDto>();
            decimal compTotal = 0;

            foreach (var line in comp.Materials)
            {
                if (line.Grams <= 0) continue;
                if (!materialsById.TryGetValue(line.MaterialId, out var mat))
                    throw new InvalidOperationException($"Vật liệu không tồn tại hoặc đã ngừng dùng: {line.MaterialId}");

                if (!mat.IsActive)
                    throw new InvalidOperationException($"Vật liệu «{mat.Name}» đã ngừng kinh doanh.");

                var priceHistory = mat.PriceHistories.FirstOrDefault(p => p.IsCurrent)
                    ?? throw new InvalidOperationException($"Vật liệu «{mat.Name}» chưa có bảng giá.");

                var pricePerGram = priceHistory.TotalServiceCostPerGram;
                var totalGrams = line.Grams * qty;
                var lineTotal = Math.Round(totalGrams * pricePerGram, 0, MidpointRounding.AwayFromZero);

                materialLines.Add(new Mainflow2QuoteMaterialLineDto
                {
                    MaterialId = mat.Id,
                    MaterialName = mat.Name,
                    GramsPerUnit = line.Grams,
                    ComponentQuantity = qty,
                    TotalGrams = totalGrams,
                    PricePerGram = pricePerGram,
                    LineTotal = lineTotal
                });
                compTotal += lineTotal;
            }

            if (materialLines.Count == 0)
                throw new InvalidOperationException($"Thành phần «{name}» cần ít nhất một dòng vật liệu có định lượng > 0.");

            materialSubtotal += compTotal;
            componentLines.Add(new Mainflow2QuoteComponentLineDto
            {
                Name = name,
                Quantity = qty,
                Materials = materialLines,
                LineTotal = compTotal
            });
        }

        if (componentLines.Count == 0)
            throw new InvalidOperationException("Báo giá cần ít nhất một thành phần sản phẩm.");

        laborCost = Math.Max(0, laborCost);
        var total = materialSubtotal + laborCost;

        return new Mainflow2QuoteBreakdownDto
        {
            Components = componentLines,
            MaterialSubtotal = materialSubtotal,
            LaborCost = laborCost,
            Total = total,
            Currency = currency
        };
    }
}
