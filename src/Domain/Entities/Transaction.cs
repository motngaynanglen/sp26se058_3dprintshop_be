using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class Transaction : BaseAuditableEntity
{
    [Required]
    public Guid InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public virtual Invoice Invoice { get; set; } = null!;

    //  [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; } // MoMo, VNPAY, BankTransfer, Cash

    [MaxLength(100)]
    public string? ExternalTransactionId { get; set; } // Mã giao dịch từ cổng thanh toán (ví dụ: mã tham chiếu MoMo)

    public string? Note { get; set; }

    [Required]
    [MaxLength(20)]
    public string TransactionStatus { get; set; } = "PENDING"; // Pending, Success, Failed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
