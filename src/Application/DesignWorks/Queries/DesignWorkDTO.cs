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
    public Guid? ParentDesignWorkId { get; set; }
    public Guid RootDesignWorkId { get; set; }
    public string? RelationshipType { get; set; }

    public string? BaseImageUrl { get; set; }
    public Guid? ResultDraftId { get; set; }
    public string? Status { get; set; }
    public bool IsLocked { get; set; }
    /// <summary>null = legacy Design Service; "DESIGN_SERVICE" or "PRINT_SERVICE"</summary>
    public string? WorkType { get; set; }


    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? MainAssignedStaffId { get; set; }
    public string? StaffName { get; set; }

    public DateTimeOffset Created { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DesignWork, DesignWorkDTO>()
                .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer.Account.Fullname)) // Giả định path tới tên
                .ForMember(d => d.StaffName, opt => opt.MapFrom(s => s.MainAssignedStaff != null ? s.MainAssignedStaff.Account.Fullname : "Chưa phân công"));
        }
    }
}
public class DesignWorkDetailDTO : DesignWorkDTO
{
    // Có thể bao gồm thêm thông tin Selection để FE hiển thị giá tiền
    public ICollection<ServiceSelectionDTO>? Selections { get; init; }
    public ICollection<DesignWorkDTO>? SubRevisions { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DesignWork, DesignWorkDetailDTO>()
            .IncludeBase<DesignWork, DesignWorkDTO>()
            .ForMember(d => d.Selections, opt => opt.MapFrom(s => s.ServiceSelections))
            .ForMember(d => d.SubRevisions, opt => opt.MapFrom(s => s.ChildDesignWorks));
        }
    }
}
