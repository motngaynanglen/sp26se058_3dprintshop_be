using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class Invoice : BaseAuditableEntity
{
    public Guid OrderId { get; set; }
    public string InvoiceCode { get; set; } = null!;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "UNPAID"; // Unpaid, Partially_Paid, Paid, Refunded
    public DateTime? DueDate { get; set; }

    // Navigation
    public virtual Order Order { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();


}
