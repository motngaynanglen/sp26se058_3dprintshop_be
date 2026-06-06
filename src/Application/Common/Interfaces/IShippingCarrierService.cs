namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

public interface IShippingCarrierService
{
    string CarrierCode { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }
    Task<ShippingQuoteDto?> GetQuoteAsync(ShippingQuoteContext context, CancellationToken cancellationToken);
    Task<CarrierShipmentCreateResult> CreateShipmentAsync(
        CarrierShipmentCreateContext context,
        CancellationToken cancellationToken);
    bool TryMapCarrierStatus(string? carrierStatus, out string shipmentStatus);
}

public interface IShippingCarrierResolver
{
    IShippingCarrierService? GetService(string carrierCode);
    IReadOnlyList<IShippingCarrierService> GetAll();
}

public record ShippingQuoteContext(
    string ReceiverName,
    string Phone,
    string AddressLine,
    string Ward,
    string District,
    string City,
    string Province,
    int WeightGrams,
    decimal OrderValue,
    bool CollectOnDelivery,
    int? GhnToDistrictId = null,
    string? GhnToWardCode = null);

public class ShippingQuoteDto
{
    public string Carrier { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public decimal Fee { get; set; }
    public int LeadDays { get; set; }
    public bool IsEstimated { get; set; }
    public string? Message { get; set; }
}

public record CarrierShipmentCreateContext(
    Guid OrderId,
    string OrderCode,
    decimal OrderValue,
    bool IsInvoicePaid,
    string ReceiverName,
    string Phone,
    string AddressLine,
    string Ward,
    string District,
    string City,
    string Province,
    int WeightGrams,
    IReadOnlyList<CarrierShipmentLineItem> Items,
    int? GhnToDistrictId = null,
    string? GhnToWardCode = null);

public record CarrierShipmentLineItem(string Name, int Quantity, decimal WeightKg);

public class CarrierShipmentCreateResult
{
    public bool Success { get; init; }
    public string? CarrierOrderCode { get; init; }
    public string? TrackingNumber { get; init; }
    public string? LabelUrl { get; init; }
    public string? CarrierStatus { get; init; }
    public string? RawJson { get; init; }
    public string? ErrorMessage { get; init; }

    public static CarrierShipmentCreateResult Ok(
        string carrierOrderCode,
        string? tracking,
        string? labelUrl,
        string? carrierStatus,
        string? rawJson) =>
        new()
        {
            Success = true,
            CarrierOrderCode = carrierOrderCode,
            TrackingNumber = tracking,
            LabelUrl = labelUrl,
            CarrierStatus = carrierStatus,
            RawJson = rawJson
        };

    public static CarrierShipmentCreateResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
