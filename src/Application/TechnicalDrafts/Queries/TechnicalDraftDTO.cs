using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.TechnicalDrafts.Queries;
public class TechnicalDraftDTO
{
    public Guid Id { get; init; }
    public Guid DesignVersionHistoryId { get; init; }
    public int VersionNumber { get; init; } // Lấy từ DesignVersionHistory

    public Guid MaterialId { get; init; }
    public string? MaterialName { get; init; } // Join sang bảng Material

    public int InfillDensity { get; init; }
    public decimal LayerHeight { get; init; }

    public decimal EstimatedWeightPerUnit { get; init; }
    public decimal? EstimatedPrintTimePerUnit { get; init; }
    public decimal MarkupPercentage { get; init; }

    // Thuộc tính tính toán (Computed Properties)
    public decimal UnitPrice { get; set; } // Giá tiền cho sản phẩm khi dựa trên vật liệu
    public decimal FinalPrice { get; init; } // Giá sau khi tính Markup

    public string? TechnicalNote { get; init; }
    public bool IsConfirmed { get; init; } // Trạng thái khách đã duyệt bản này chưa
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TechnicalDraft, TechnicalDraftDTO>()
                .ForMember(d => d.VersionNumber, opt => opt.MapFrom(s => s.DesignVersionHistory.VersionNumber))
                .ForMember(d => d.MaterialName, opt => opt.MapFrom(s => s.Material.Name))
                // Bug fix: was (1 + MarkupPercentage) which multiplied by e.g. 16 instead of 1.15
                // Correct formula: UnitPrice * (1 + MarkupPercentage / 100)
                .ForMember(d => d.FinalPrice, opt => opt.MapFrom(s => s.UnitPrice * (1 + s.MarkupPercentage / 100m)));

        }
    }
}
