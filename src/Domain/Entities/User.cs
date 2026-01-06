using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class User : BaseAuditableEntity // Kế thừa để có CreatedBy, LastModifiedBy
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!; // Luôn lưu Hash, không lưu text
    public string Role { get; set; } = "Customer";
}
