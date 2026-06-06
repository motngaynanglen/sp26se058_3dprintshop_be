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
    public Guid DesignVariantId { get; init; }
    public string? VariantName { get; init; }
    public int Quantity { get; init; }
    public string? Type { get; init; }
    public string? TypeLabel { get; init; }
    public string? TypeColor { get; init; }
    public bool IsInbound { get; init; }
    public string? StaffName { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? Note { get; init; }
    public DateTimeOffset Created { get; init; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<InventoryTransaction, InventoryTransactionDTO>()
                    .ForMember(d => d.VariantName, opt => opt.MapFrom(s => s.DesignVariant.Name))
                    .ForMember(d => d.StaffName, opt => opt.MapFrom(s =>
                        s.Staff != null && s.Staff.Account != null
                        ? s.Staff.Account.Fullname
                        : "Hệ thống"))
                    .ForMember(d => d.TypeLabel, opt => opt.MapFrom(s =>
                        InventoryTransactionTypes.Resolve(s.Type) != null
                            ? InventoryTransactionTypes.Resolve(s.Type)!.Label
                            : s.Type))
                    .ForMember(d => d.TypeColor, opt => opt.MapFrom(s =>
                        InventoryTransactionTypes.Resolve(s.Type) != null
                            ? InventoryTransactionTypes.Resolve(s.Type)!.Color
                            : "#757575"))
                    .ForMember(d => d.IsInbound, opt => opt.MapFrom(s => InventoryTransactionTypes.IsInbound(s.Quantity)));
        }
    }
}
