using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class Staff : BaseAuditableEntity
{
    public Guid AccountId { get; set; }

    // Navigation hỗ trợ các bảng nghiệp vụ khác
    public virtual Account Account { get; set; } = null!;
    public virtual ICollection<Order> AssignedOrders { get; set; } = new List<Order>();
    public virtual ICollection<DesignWork> AssignedDesignWorks { get; set; } = new List<DesignWork>();
}
