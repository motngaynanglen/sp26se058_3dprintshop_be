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
    [MaxLength(20)]
    [Required]
    public string Username { get; set; } = null!;
    [MaxLength(40)]
    public string Fullname { get; set; } = null!;
    [MaxLength(40)]
    public string Email { get; set; } = null!;
    [MaxLength(255)]
    public string? Profile_Image_URL { get; set; }
    [MaxLength(15)]
    public string? Contact_Phone { get; set; } 
    [MaxLength(15)]
    public string? Zalo_Phone { get; set; }
    [MaxLength(255)]
    public string Password_Hash { get; set; } = null!;
    public bool Is_active { get; set; } = true;

    // Navigation properties (Mối quan hệ 1:1)
    public virtual Staff? Staff { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual Manager? Manager { get; set; }
}
