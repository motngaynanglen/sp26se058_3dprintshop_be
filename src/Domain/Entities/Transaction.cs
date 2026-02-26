using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class Transaction : BaseAuditableEntity
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public required string PaymentMethod { get; set; } // MoMo, VNPAY, BankTransfer, Cash
    public string? ExternalTransactionId { get; set; } // Mã giao dịch từ cổng thanh toán (ví dụ: mã tham chiếu MoMo)
    public string? Note { get; set; }
    public required string TransactionStatus { get; set; } = "PENDING"; // Pending, Success, Failed

    public required virtual Invoice Invoice { get; set; } = null!;

}
