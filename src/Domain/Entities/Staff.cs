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

    [MaxLength(255)]
    public string Role { get; set; } = null!; // Vai trò riêng của nhân viên

    [ForeignKey(nameof(AccountId))]
    public Account Account { get; set; } = null!;
}
