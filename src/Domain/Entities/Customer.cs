using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class Customer : BaseAuditableEntity
{

    public Guid AccountId { get; init; }
    public virtual Account Account { get; init; } = null!;
    public DateTime? DateOfBirth { get; set; }
    // Sau này có thể thêm point / token AI các thứ

    //Nativigate
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    //public virtual ICollection<ShippingAddress> ShippingAddresses { get; set; } = new List<ShippingAddress>();
    public virtual ICollection<DesignWork> DesignWorks { get; set; } = new List<DesignWork>();
}
