namespace sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;

public class PayOsPaymentLinkStatusResult
{
    public long OrderCode { get; init; }
    public long Amount { get; init; }
    public long AmountPaid { get; init; }
    public long AmountRemaining { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? PaymentLinkId { get; init; }
    public string? Reference { get; init; }
    public string? TransactionDateTime { get; init; }
}
