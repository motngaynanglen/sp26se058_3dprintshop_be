using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.ServiceSelections.Queries;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
public class DesignWorkDTO
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? SourceType { get; set; }
    //public Guid? TemplateId { get; set; }

    public string? Status { get; set; }
    public string? BaseImageUrl { get; set; }
    //public Guid? ServiceSelectionId { get; set; }
    //public Guid? ResultDraftId { get; set; }

    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? MainAssignedStaffId { get; set; }
    public string? StaffName { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DesignWork, DesignWorkDTO>()
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer.Account.Fullname))
            .ForMember(d => d.StaffName, opt => opt.MapFrom(s => s.MainAssignedStaff != null
                ? s.MainAssignedStaff.Account.Fullname
                : null));
        }
    }
}
public class DesignWorkDetailDTO : DesignWorkDTO
{
    public Guid? ServiceSelectionId { get; set; }
    public Guid? ResultDraftId { get; set; }

    // Có thể bao gồm thêm thông tin Selection để FE hiển thị giá tiền
    public ICollection<ServiceSelectionDTO>? Selections { get; init; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DesignWork, DesignWorkDetailDTO>()
            .IncludeBase<DesignWork, DesignWorkDTO>()
            .ForMember(d => d.Selections, opt => opt.MapFrom(s => s.ServiceSelections));
        }
    }
}
