using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
public class FeedbackDTO
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid DesignTemplateId { get; set; }
    public string? DesignTemplateName { get; set; }

    // OrderItem liên quan
    public Guid OrderItemId { get; set; }
    public string? OrderItemName { get; set; }
    public string? OrderItemSourceType { get; set; }

    // Nhân viên đảm nhiệm (đơn thiết kế / in custom)
    public bool HasAssignedStaffInfo { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }

    // Đánh giá
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? StaffReply { get; set; }

    // Thông tin định danh người dùng (Lấy từ Account thông qua Customer)
    public string CustomerFullName { get; set; } = string.Empty;
    /// <summary>Tên đầy đủ (không che) — dùng cho Manager/Staff.</summary>
    public string? CustomerRealName { get; set; }
    public string? CustomerAvatar { get; set; }

    // Trạng thái & Thời gian
    public bool IsHidden { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset LastModified { get; set; }

    // Danh sách ảnh thực tế của sản phẩm
    public List<string> ImageUrls { get; set; } = new();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Feedback, FeedbackDTO>()
            .ForMember(dest => dest.DesignTemplateName, opt => opt.MapFrom(src => src.DesignTemplate.Name))
            .ForMember(dest => dest.OrderItemId, opt => opt.MapFrom(src => src.OrderItemId))
            .ForMember(dest => dest.OrderItemName, opt => opt.MapFrom(src => ResolveOrderItemName(src)))
            .ForMember(dest => dest.OrderItemSourceType, opt => opt.MapFrom(src => src.OrderItem.SourceType))
            .ForMember(dest => dest.HasAssignedStaffInfo, opt => opt.MapFrom(src => HasAssignedStaffInfo(src.OrderItem.SourceType)))
            .ForMember(dest => dest.AssignedStaffId, opt => opt.MapFrom(src => ResolveAssignedStaffId(src)))
            .ForMember(dest => dest.AssignedStaffName, opt => opt.MapFrom(src => ResolveAssignedStaffName(src)))
            .ForMember(dest => dest.CustomerFullName, opt => opt.MapFrom(src =>
                src.Customer.Account != null
                    ? MaskName(src.Customer.Account.Fullname ?? src.Customer.Account.Username)
                    : "Người dùng ẩn danh"))
            .ForMember(dest => dest.CustomerRealName, opt => opt.MapFrom(src =>
                src.Customer.Account != null
                    ? (src.Customer.Account.Fullname ?? src.Customer.Account.Username)
                    : null))
            .ForMember(dest => dest.CustomerAvatar, opt => opt.MapFrom(src =>
                src.Customer.Account != null ? src.Customer.Account.ProfileImageURL : null))
            .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src =>
                src.FeedbackImages.Select(x => x.ImageUrl).ToList()));
        }

        private static bool HasAssignedStaffInfo(string? sourceType) =>
            SourceTypes.IsCustomPrintFlow(sourceType);

        private static string ResolveOrderItemName(Feedback src)
        {
            var item = src.OrderItem;
            if (!string.IsNullOrWhiteSpace(item?.ItemName))
                return item.ItemName!;

            if (item?.DesignVariant != null)
                return item.DesignVariant.Name;

            if (!string.IsNullOrWhiteSpace(item?.DesignWork?.Name))
                return item.DesignWork!.Name!;

            return src.DesignTemplate?.Name ?? "Sản phẩm";
        }

        private static Guid? ResolveAssignedStaffId(Feedback src)
        {
            if (!HasAssignedStaffInfo(src.OrderItem?.SourceType))
                return null;

            return src.OrderItem?.DesignWork?.MainAssignedStaffId
                ?? src.OrderItem?.Order?.StaffId;
        }

        private static string? ResolveAssignedStaffName(Feedback src)
        {
            if (!HasAssignedStaffInfo(src.OrderItem?.SourceType))
                return null;

            var designStaff = src.OrderItem?.DesignWork?.MainAssignedStaff?.Account;
            if (designStaff != null)
                return designStaff.Fullname ?? designStaff.Username;

            var orderStaff = src.OrderItem?.Order?.Staff?.Account;
            if (orderStaff != null)
                return orderStaff.Fullname ?? orderStaff.Username;

            return null;
        }

        private static string MaskName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length <= 2) return name;
            return name[0] + new string('*', name.Length - 2) + name[^1];
        }
    }
}
