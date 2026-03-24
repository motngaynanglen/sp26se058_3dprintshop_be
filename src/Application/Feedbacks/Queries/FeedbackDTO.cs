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

    // Thông tin định danh người dùng (Lấy từ Account thông qua Customer)
    public string CustomerFullName { get; set; } = string.Empty;
    public string? CustomerAvatar { get; set; }

    // Trạng thái & Thời gian
    public bool IsHidden { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }

    // Danh sách ảnh thực tế của sản phẩm
    public List<string> ImageUrls { get; set; } = new();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Feedback, FeedbackDTO>()
            // Lấy Tên hiển thị: Ưu tiên Fullname, nếu null lấy Username
            // Thêm logic ẩn danh ở đây: "Nguyễn Văn A" -> "N*** A"
            .ForMember(dest => dest.CustomerFullName, opt => opt.MapFrom(src =>
                src.Customer.Account != null ? (MaskName(src.Customer.Account.Fullname ?? src.Customer.Account.Username)) : "Người dùng ẩn danh"))

            // Lấy Avatar từ Account
            .ForMember(dest => dest.CustomerAvatar, opt => opt.MapFrom(src =>
                src.Customer.Account != null ? src.Customer.Account.ProfileImageURL : null))

            // Map danh sách URL ảnh từ tập hợp FeedbackImages
            .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src =>
                src.FeedbackImages.Select(x => x.ImageUrl).ToList()));
        }
        private string MaskName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length <= 2) return name;
            return name[0] + new string('*', name.Length - 2) + name[^1];
        }
    }
}
