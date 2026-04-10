using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.PackageOptions.Queries;
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
