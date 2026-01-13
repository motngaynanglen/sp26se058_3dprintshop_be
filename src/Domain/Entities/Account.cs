using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class Account : BaseAuditableEntity // Kế thừa để có CreatedBy, LastModifiedBy
{
    public string Username { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Profile_Image_URL { get; set; }
    public string Contact_Phone { get; set; } = string.Empty;
    public string? Zalo_Phone { get; set; }
    public string Password_Hash { get; set; } = string.Empty;
    public bool Is_active { get; set; } = true;

    // Navigation properties (Mối quan hệ 1:1)
    //public virtual Staff? Staff { get; set; }
    //public virtual Customer? Customer { get; set; }
    //public virtual Manager? Manager { get; set; }
}
