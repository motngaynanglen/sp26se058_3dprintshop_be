using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class Shipment : BaseAuditableEntity
{
    public Guid OrderId { get; set; }
    public Guid ShippingAddressId { get; set; }

    public decimal ShippingFee { get; set; }

    public string RecipientName { get; set; } = null!;
    public string RecipientPhone { get; set; } = null!;
    public string AddressLine { get; set; } = null!;
    public string Ward { get; set; } = null!;
    public string District { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Province { get; set; } = null!;

    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
    public string ShipmentStatus { get; set; } = "PREPARING";
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Note { get; set; }

    // Navigation Properties
    public virtual Order Order { get; set; } = null!;
    public virtual ShippingAddress ShippingAddress { get; set; } = null!;
    public virtual ICollection<ShipmentAddressChangeRequest> AddressChangeRequests { get; set; } = new List<ShipmentAddressChangeRequest>();
    //public virtual ShippingMethod ShippingMethod { get; set; } = null!;
}
