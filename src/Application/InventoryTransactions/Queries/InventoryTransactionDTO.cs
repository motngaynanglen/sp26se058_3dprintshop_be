using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.InventoryTransactions.Queries;
public class InventoryTransactionDTO
{
    public Guid Id { get; init; }
    public string? VariantName { get; init; } // Tên sản phẩm để hiển thị
    public int Quantity { get; init; }
    public string? Type { get; init; }
    public string? TypeLabel { get; init; } // Lấy từ Constant/Extension "Nhập mua", "Xuất bán"
    public string? TypeColor { get; init; } // #4CAF50...
    public string? StaffName { get; init; } // Tên người thực hiện
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<InventoryTransaction, InventoryTransactionDTO>()
                    .ForMember(d => d.VariantName, opt => opt.MapFrom(s => s.DesignVariant.Name))
                    // Kiểm tra null cho Staff và Account để lấy Fullname
                    .ForMember(d => d.StaffName, opt => opt.MapFrom(s =>
                        s.Staff != null && s.Staff.Account != null
                        ? s.Staff.Account.Fullname
                        : "Hệ thống"))
                    // Lấy Metadata từ danh sách Constant All
                    .ForMember(d => d.TypeLabel, opt => opt.MapFrom(s =>
                        InventoryTransactionTypes.All.FirstOrDefault(t => t.Value == s.Type)!.Label))
                    .ForMember(d => d.TypeColor, opt => opt.MapFrom(s =>
                        InventoryTransactionTypes.All.FirstOrDefault(t => t.Value == s.Type)!.Color))
                    .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.Created));
        }
    }
}
