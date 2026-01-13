using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class Staff : BaseAuditableEntity
{
    public Guid AccountId { get; set; }
    public string Role { get; set; } = "Staff"; // Ví dụ: Designer, Technician

    // Navigation property
    public virtual Account Account { get; set; } = null!;
}
