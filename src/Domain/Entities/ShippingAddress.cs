using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class ShippingAddress : BaseAuditableEntity
{
    public Guid CustomerId { get; set; }
    public string ReceiverName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string AddressLine { get; set; } = null!;
    public string Ward { get; set; } = null!;
    public string District { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Province { get; set; } = null!;

    /// <summary>Mã quận/huyện GHN (district_id) — dùng tính phí & tạo vận đơn.</summary>
    public int? GhnDistrictId { get; set; }

    /// <summary>Mã phường/xã GHN (ward_code).</summary>
    public string? GhnWardCode { get; set; }

    public bool IsDefault { get; set; } = false;
    public required virtual Customer Customer { get; set; } = null!;
}
