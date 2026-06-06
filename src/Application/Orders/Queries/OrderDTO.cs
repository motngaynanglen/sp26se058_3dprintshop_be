using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Orders.Queries;

public class OrderDTO
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? StaffId { get; set; }
    public decimal TotalPrice { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? DepositedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public List<OrderItemDTO> OrderItems { get; set; } = new();
    public OrderShipmentSummaryDTO? Shipment { get; set; }
    public OrderInvoiceSummaryDTO? Invoice { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Order, OrderDTO>()
                .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer.Account.Fullname))
                .ForMember(d => d.Shipment, opt => opt.Ignore())
                .ForMember(d => d.Invoice, opt => opt.Ignore());
            CreateMap<OrderItem, OrderItemDTO>()
                .ForMember(d => d.ThumbnailUrl, opt => opt.MapFrom(s =>
                    s.DesignVariant != null && s.DesignVariant.DesignTemplate != null
                        ? s.DesignVariant.DesignTemplate.ThumbnailUrl
                        : s.DesignWork != null
                            ? s.DesignWork.BaseImageUrl
                            : null));
            CreateMap<Shipment, OrderShipmentSummaryDTO>()
                .ForMember(d => d.FullAddress, opt => opt.Ignore())
                .AfterMap((src, dest) => dest.FullAddress = FormatFullAddress(src.ShippingAddress));
            CreateMap<Invoice, OrderInvoiceSummaryDTO>();
        }

        private static string? FormatFullAddress(ShippingAddress? address)
        {
            if (address == null) return null;
            var parts = new[] { address.AddressLine, address.Ward, address.District, address.City }
                .Where(str => !string.IsNullOrWhiteSpace(str));
            return string.Join(", ", parts);
        }
    }
}

public class OrderItemDTO
{
    public Guid Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? DesignVariantId { get; set; }
    public Guid? DesignWorkId { get; set; }
    public string? ItemName { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int QuantityOrdered { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string FulfillmentStatus { get; set; } = string.Empty;
    public bool CanSubmitFeedback { get; set; }
    public OrderItemFeedbackDto? Feedback { get; set; }
}

public class OrderItemFeedbackDto
{
    public Guid Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? StaffReply { get; set; }
    public DateTimeOffset Created { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

public class OrderShipmentSummaryDTO
{
    public Guid Id { get; set; }
    public string ShipmentStatus { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? CarrierOrderCode { get; set; }
    public string? CarrierStatus { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
    public string? FullAddress { get; set; }
}

public class OrderInvoiceSummaryDTO
{
    public Guid Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    /// <summary>Tiền thiết kế (custom MF2) — dùng tính cọc 30%.</summary>
    public decimal? DesignFeeAmount { get; set; }
    /// <summary>Số tiền cọc dự kiến (30% tiền thiết kế).</summary>
    public decimal? CustomDepositAmount { get; set; }
    /// <summary>Phần còn lại của tổng đơn sau cọc.</summary>
    public decimal? RemainingBalance { get; set; }
    /// <summary>PAYOS | VNPAY | CASH (COD) — từ giao dịch gần nhất.</summary>
    public string? PaymentMethod { get; set; }
    public bool IsCod { get; set; }
}
