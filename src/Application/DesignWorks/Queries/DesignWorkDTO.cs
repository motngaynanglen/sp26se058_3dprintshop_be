using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
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

    /// <summary>
    /// True khi phí dịch vụ thiết kế đã được thanh toán (Status đã qua PENDING).
    /// FE dùng để: bật/tắt nút "Thanh toán phí thiết kế", gate staff tạo báo giá.
    /// </summary>
    public bool IsDesignServicePaid =>
        Status is "IN_PROGRESS" or "REVIEWING" or "COMPLETED";
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
    // Danh sách gói dịch vụ đã chọn/đã mua (kèm snapshot từng option).
    public ICollection<ServiceSelectionDTO>? Selections { get; init; }
    public ICollection<DesignWorkDTO>? SubRevisions { get; set; }

    // Hóa đơn + thông tin đơn phí dịch vụ thiết kế (nếu đã tạo đơn thanh toán).
    public OrderInvoiceSummaryDTO? Invoice { get; set; }
    public Guid? DesignServiceOrderId { get; set; }
    public string? DesignServiceOrderCode { get; set; }
    public string? DesignServiceOrderStatus { get; set; }
    public string? DesignServicePaymentStatus { get; set; }
    public decimal? DesignServiceTotalAmount { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DesignWork, DesignWorkDetailDTO>()
            .IncludeBase<DesignWork, DesignWorkDTO>()
            .ForMember(d => d.Selections, opt => opt.MapFrom(s => s.ServiceSelections))
            .ForMember(d => d.SubRevisions, opt => opt.MapFrom(s => s.ChildDesignWorks))
            .ForMember(d => d.Invoice, opt => opt.Ignore())
            .ForMember(d => d.DesignServiceOrderId, opt => opt.Ignore())
            .ForMember(d => d.DesignServiceOrderCode, opt => opt.Ignore())
            .ForMember(d => d.DesignServiceOrderStatus, opt => opt.Ignore())
            .ForMember(d => d.DesignServicePaymentStatus, opt => opt.Ignore())
            .ForMember(d => d.DesignServiceTotalAmount, opt => opt.Ignore());
        }
    }
}
