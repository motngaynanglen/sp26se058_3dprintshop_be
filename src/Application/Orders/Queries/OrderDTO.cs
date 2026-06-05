using AutoMapper;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Orders.Queries;

public class OrderDTO
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public int TotalItem { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public int Priority { get; set; }
    public DateTimeOffset? DepositedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTime? PaymentDueDate { get; set; }
    public DateTimeOffset? Created { get; set; }
    public string? Note { get; set; }

    public List<OrderItemDTO> Items { get; set; } = new();

    // Enriched — nested summaries
    public OrderInvoiceSummaryDTO? Invoice { get; set; }
    public OrderShipmentSummaryDTO? Shipment { get; set; }
    public string? ShippingAddress { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Order, OrderDTO>()
                .ForMember(d => d.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null && src.Customer.Account != null
                        ? src.Customer.Account.Fullname : string.Empty))
                .ForMember(d => d.Items,
                    opt => opt.MapFrom(src => src.OrderItems))
                .ForMember(d => d.TotalItem,
                    opt => opt.MapFrom(src => src.OrderItems.Sum(oi => oi.QuantityOrdered)))
                .ForMember(d => d.PaymentDueDate,
                    opt => opt.MapFrom(src => src.Invoice != null ? src.Invoice.DueDate : null))
                .ForMember(d => d.SourceType,
                    opt => opt.MapFrom(src => src.OrderItems.Any()
                        ? src.OrderItems.First().SourceType : null))
                .ForMember(d => d.Invoice, opt => opt.Ignore())
                .ForMember(d => d.Shipment, opt => opt.Ignore())
                .ForMember(d => d.ShippingAddress, opt => opt.Ignore());
        }
    }
}

public class OrderItemDTO
{
    public Guid Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public Guid? DesignVariantId { get; set; }
    public Guid? DesignWorkId { get; set; }
    public int QuantityOrdered { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string FulfillmentStatus { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? MaterialName { get; set; }
    public decimal? EstimatedWeightPerUnit { get; set; }

    // Feedback
    public bool CanSubmitFeedback { get; set; }
    public OrderItemFeedbackDto? Feedback { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<OrderItem, OrderItemDTO>()
                .ForMember(d => d.SourceType,
                    opt => opt.MapFrom(src => src.SourceType.ToString()))
                .ForMember(d => d.FulfillmentStatus,
                    opt => opt.MapFrom(src => src.FulfillmentStatus.ToString()))
                .ForMember(d => d.ThumbnailUrl,
                    opt => opt.MapFrom(src => src.DesignVariant != null
                        ? (src.DesignVariant.PreviewModelUrl ?? (src.DesignVariant.DesignTemplate != null
                            ? src.DesignVariant.DesignTemplate.ThumbnailUrl : null))
                        : null))
                .ForMember(d => d.MaterialName,
                    opt => opt.MapFrom(src => src.DesignVariant != null && src.DesignVariant.Material != null
                        ? src.DesignVariant.Material.Name : null))
                .ForMember(d => d.EstimatedWeightPerUnit,
                    opt => opt.MapFrom(src => src.DesignVariant != null
                        ? src.DesignVariant.EstimatedWeightPerUnit : null))
                .ForMember(d => d.CanSubmitFeedback, opt => opt.Ignore())
                .ForMember(d => d.Feedback, opt => opt.Ignore());
        }
    }
}

// ─── Nested Summary DTOs ─────────────────────────────────
public class OrderInvoiceSummaryDTO
{
    public Guid Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string? PaymentMethod { get; set; }
}

public class OrderShipmentSummaryDTO
{
    public Guid Id { get; set; }
    public string ShipmentStatus { get; set; } = string.Empty;
    public string? CarrierName { get; set; }
    public string? Carrier { get; set; }
    public string? CarrierOrderCode { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal ShippingFee { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? FullAddress { get; set; }
}

public class OrderItemFeedbackDto
{
    public Guid Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? StaffReply { get; set; }
    public DateTimeOffset? Created { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}
