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
    public Guid? ShippingMethodId { get; set; }

    public decimal ShippingFee { get; set; }
    public string? TrackingNumber { get; set; }

    /// <summary>MANUAL, GHN — <see cref="Constants.Types.ShippingCarriers"/>.</summary>
    public string? Carrier { get; set; }

    /// <summary>Mã đơn trên hệ thống GHN (order_code).</summary>
    public string? CarrierOrderCode { get; set; }

    /// <summary>Trạng thái thô từ đơn vị vận chuyển.</summary>
    public string? CarrierStatus { get; set; }

    public string? CarrierLabelUrl { get; set; }
    public string? CarrierMetaJson { get; set; }

    public DateTime? EstimatedDeliveryTime { get; set; }
    public string ShipmentStatus { get; set; } = "PENDING";
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // Navigation Properties
    public virtual Order Order { get; set; } = null!;
    public virtual ShippingAddress ShippingAddress { get; set; } = null!;
    //public virtual ShippingMethod ShippingMethod { get; set; } = null!;
}
