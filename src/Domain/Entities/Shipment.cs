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

    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
    public string ShipmentStatus { get; set; } = "PENDING";
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // Navigation Properties
    public virtual Order Order { get; set; } = null!;
    public virtual ShippingAddress ShippingAddress { get; set; } = null!;
    //public virtual ShippingMethod ShippingMethod { get; set; } = null!;
}
