using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class Order : BaseAuditableEntity
{
    [Required]
    public Guid CustomerId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    [MaxLength(20)]
    public string OrderStatus { get; set; } = "Pending";

    public int Priority { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // 1-1 Relationship với Invoice
    public virtual Invoice? Invoice { get; set; }
}
