namespace sp26se058_3dprintshop_be.Application.Orders.Models;

public class ProductionQueueOrderDto
{
    public Guid OrderId { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string OrderStatus { get; init; } = string.Empty;
    public string? ShipmentStatus { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
    public DateTimeOffset? Created { get; init; }
    public decimal TotalPrice { get; init; }
    public IReadOnlyList<ProductionQueueLineDto> Lines { get; init; } = Array.Empty<ProductionQueueLineDto>();
    public bool AllLinesFinished { get; init; }
    public int PendingPrintCount { get; init; }
}

public class ProductionQueueLineDto
{
    public Guid OrderItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;
    public string FulfillmentStatus { get; init; } = string.Empty;
    public int QuantityOrdered { get; init; }
    public Guid? DesignWorkId { get; init; }
}
