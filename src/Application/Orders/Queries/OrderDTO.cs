using AutoMapper;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Orders.Queries;

public class OrderDTO
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public int TotalItem { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTimeOffset? DepositedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? Created { get; set; }

    public List<OrderItemDTO> Items { get; set; } = new();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Order, OrderDTO>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer.Account.Fullname))
                .ForMember(dest => dest.Items,
                    opt => opt.MapFrom(src => src.OrderItems))
                .ForMember(dest => dest.TotalItem,
                    opt => opt.MapFrom(src => src.OrderItems.Sum(oi => oi.QuantityOrdered)));
        }
        
    }
}

public class OrderItemDTO
{
    public Guid Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public Guid? DesignVariantId { get; set; }
    public int QuantityOrdered { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string FulfillmentStatus { get; set; } = string.Empty;

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<OrderItem, OrderItemDTO>()
                .ForMember(dest => dest.SourceType,
                    opt => opt.MapFrom(src => src.SourceType.ToString()))
                .ForMember(dest => dest.FulfillmentStatus,
                    opt => opt.MapFrom(src => src.FulfillmentStatus.ToString()));
        }
    }
}
