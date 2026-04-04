using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ServicePackages.Queries;
public class ServicePackageDTO
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ServiceType { get; set; } = null!; // DESIGN | PRINTING
    public decimal BasePrice { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? Created { get; set; }
    // Danh sách các option khả dụng cho gói này
    public List<PackageOptionDTO> PackageOptions { get; set; } = new();
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ServicePackage, ServicePackageDTO>()
            .ForMember(dest => dest.PackageOptions, opt => opt.MapFrom(src => src.PackageOptions));
        }
    }
}
public class PackageOptionDTO
{
    public Guid Id { get; set; }
    public Guid ServiceOptionId { get; set; }

    // Thông tin chi tiết từ bảng ServiceOption (Flattening)
    public string OptionName { get; set; } = null!;
    public string OptionCode { get; set; } = null!;
    public string OptionType { get; set; } = null!;

    // Thông tin cấu hình trong gói
    public bool IsRequired { get; set; }
    public bool DefaultSelected { get; set; }
    public decimal AppliedPrice { get; set; } // Giá cuối cùng (đã check Override)

    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<PackageOption, PackageOptionDTO>()
            // Lấy thông tin từ ServiceOption liên kết
            .ForMember(dest => dest.OptionName, opt => opt.MapFrom(src => src.ServiceOption.Name))
            .ForMember(dest => dest.OptionCode, opt => opt.MapFrom(src => src.ServiceOption.Code))
            .ForMember(dest => dest.OptionType, opt => opt.MapFrom(src => src.ServiceOption.OptionType))

            // Logic tính giá hiển thị: Nếu có Override thì dùng, không thì dùng mặc định
            .ForMember(dest => dest.AppliedPrice, opt => opt.MapFrom(src =>
                src.PriceOverride ?? src.ServiceOption.DefaultPrice));
        }
    }
}
