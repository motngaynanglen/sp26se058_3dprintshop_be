using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
public class FeedbackDTO
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid DesignTemplateId { get; set; }

    // Đánh giá
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? StaffReply { get; set; }
    public DateTimeOffset? RepliedDate { get; set; }
    // Thông tin định danh người dùng (Lấy từ Account thông qua Customer)
    public string? RawCustomerName { get; init; }
    public string CustomerFullName => MaskName(RawCustomerName ?? "Người dùng ẩn danh");
    public string? CustomerAvatar { get; set; }
    public Guid? AccountId { get; set; } // để hỗ trợ FE cho người dùng biết đâu là của bản thân
    // Trạng thái & Thời gian
    public bool IsHidden { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? LastModified { get; set; }

    // Danh sách ảnh thực tế của sản phẩm
    public List<string> ImageUrls { get; set; } = new();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Feedback, FeedbackDTO>()
            // Lấy Tên hiển thị: Ưu tiên Fullname, nếu null lấy Username
            // Thêm logic ẩn danh ở đây: "Nguyễn Văn A" -> "N*** A"
            .ForMember(dest => dest.RawCustomerName, opt => opt.MapFrom(src =>
                src.Customer.Account.Fullname ?? src.Customer.Account.Username))

            .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => 
                src.Customer != null ? src.Customer.AccountId : (Guid?)null))
            // Lấy Avatar từ Account
            .ForMember(dest => dest.CustomerAvatar, opt => opt.MapFrom(src =>
                src.Customer.Account != null ? src.Customer.Account.ProfileImageURL : null))

            // Map danh sách URL ảnh từ tập hợp FeedbackImages
            .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src =>
                src.FeedbackImages.Select(x => x.ImageUrl).ToList()));
        }

    }
    private string MaskName(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "Người dùng ẩn danh") return name;
        if (name.Length <= 2) return name;
        return name[2] + new string('*', name.Length - 4) + name[^1];
    }
}
