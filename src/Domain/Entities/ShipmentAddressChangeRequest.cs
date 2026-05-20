namespace sp26se058_3dprintshop_be.Domain.Entities;

public class ShipmentAddressChangeRequest : BaseAuditableEntity
{
    public Guid ShipmentId { get; set; }
    public Guid RequestedByCustomerId { get; set; }
    public Guid NewShippingAddressId { get; set; }
    public Guid? ReviewedByAccountId { get; set; }

    public string Status { get; set; } = "PENDING";
    public string Reason { get; set; } = null!;
    public string? ResponseNote { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    public virtual Shipment Shipment { get; set; } = null!;
    public virtual Customer RequestedByCustomer { get; set; } = null!;
    public virtual ShippingAddress NewShippingAddress { get; set; } = null!;
    public virtual Account? ReviewedByAccount { get; set; }
}
