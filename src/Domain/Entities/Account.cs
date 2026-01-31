using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class Account : BaseAuditableEntity // Kế thừa để có CreatedBy, LastModifiedBy
{
    public required string Username { get; set; }
    public required string Fullname { get; set; }
    public required string Email { get; set; }
    public string? ProfileImageURL { get; set; }
    public string? ContactPhone { get; set; }
    public string? ZaloPhone { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    // Navigation properties (Mối quan hệ 1:1)
    public virtual Staff? Staff { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual Manager? Manager { get; set; }
}
