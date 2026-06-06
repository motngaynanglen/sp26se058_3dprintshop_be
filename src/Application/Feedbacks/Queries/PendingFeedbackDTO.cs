using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
public class PendingFeedbackDTO
{
    // ID của OrderItem để khi gửi Feedback ta biết món nào
    public Guid OrderItemId { get; set; }

    // Thông tin sản phẩm để hiển thị trên UI
    public string ProductName { get; set; } = string.Empty;
    public string? VariantInfo { get; set; }
    public string? ImageUrl { get; set; }

    // Thông tin bổ sung (tùy chọn)
    public decimal Price { get; set; }
    public DateTime OrderDate { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<OrderItem, PendingFeedbackDTO>()
            // Xử lý tên sản phẩm: Nếu không có Variant, lấy từ thông tin khác (ví dụ ProductName lưu trực tiếp ở OrderItem)
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s =>
                s.DesignVariant != null
                ? s.DesignVariant.DesignTemplate.Name
                : ("Sản phẩm tùy chỉnh"))) // Fallback nếu Variant null

            // Xử lý thông tin Variant
            .ForMember(d => d.VariantInfo, opt => opt.MapFrom(s =>
                s.DesignVariant != null ? s.DesignVariant.Name : "Yêu cầu riêng"))

            // Xử lý ảnh: Lấy ảnh Variant -> Ảnh Template -> Ảnh mặc định
            .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s =>
                s.DesignVariant != null
                ? (s.DesignVariant.PreviewModelUrl ?? s.DesignVariant.DesignTemplate.ThumbnailUrl)
                : "")) // Giả sử bạn có lưu ảnh riêng cho món hàng custom

            .ForMember(d => d.Price, opt => opt.MapFrom(s => s.UnitPrice))
            .ForMember(d => d.OrderDate, opt => opt.MapFrom(s => s.Order.Created.DateTime));
        }
    }
}
