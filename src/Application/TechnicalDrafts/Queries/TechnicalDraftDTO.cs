using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.TechnicalDrafts.Queries;
public class TechnicalDraftDTO
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public Guid DesignVersionHistoryId { get; init; }
    public int VersionNumber { get; init; } // Lấy từ DesignVersionHistory
    public Guid DesignWorkId { get; init; }
    public string? DesignWorkName { get; init; }
    public string? BaseImageUrl { get; init; }
    public string? DesignVersionFileUrl { get; init; }
    public string? PreviewModelUrl { get; init; }

    public Guid MaterialId { get; init; }
    public string? MaterialName { get; init; } // Join sang bảng Material
    public decimal? MaterialBaseCostPerGram { get; init; }
    public decimal? MaterialTotalServiceCostPerGram { get; init; }
    public DateTime? MaterialPriceEffectiveDate { get; init; }
    public DateTimeOffset? LastPriceUpdatedAt { get; init; }

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
    public DateTimeOffset? ConfirmedAt { get; init; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TechnicalDraft, TechnicalDraftDTO>()
                .ForMember(d => d.VersionNumber, opt => opt.MapFrom(s => s.DesignVersionHistory.VersionNumber))
                .ForMember(d => d.DesignWorkId, opt => opt.MapFrom(s => s.DesignVersionHistory.DesignWorkId))
                .ForMember(d => d.DesignWorkName, opt => opt.MapFrom(s => s.DesignVersionHistory.DesignWork.Name))
                .ForMember(d => d.BaseImageUrl, opt => opt.MapFrom(s => s.DesignVersionHistory.DesignWork.BaseImageUrl))
                .ForMember(d => d.DesignVersionFileUrl, opt => opt.MapFrom(s => s.DesignVersionHistory.FileUrl))
                .ForMember(d => d.PreviewModelUrl, opt => opt.MapFrom(s => s.PreviewModelUrl ?? s.DesignVersionHistory.FileUrl))
                .ForMember(d => d.MaterialName, opt => opt.MapFrom(s => s.Material.Name))
                .ForMember(d => d.MaterialBaseCostPerGram, opt => opt.MapFrom(s => s.Material.PriceHistories
                    .Where(p => p.IsCurrent)
                    .Select(p => (decimal?)p.BaseCostPerGram)
                    .FirstOrDefault()))
                .ForMember(d => d.MaterialTotalServiceCostPerGram, opt => opt.MapFrom(s => s.Material.PriceHistories
                    .Where(p => p.IsCurrent)
                    .Select(p => (decimal?)p.TotalServiceCostPerGram)
                    .FirstOrDefault()))
                .ForMember(d => d.MaterialPriceEffectiveDate, opt => opt.MapFrom(s => s.Material.PriceHistories
                    .Where(p => p.IsCurrent)
                    .Select(p => (DateTime?)p.EffectiveDate)
                    .FirstOrDefault()))
                .ForMember(d => d.LastPriceUpdatedAt, opt => opt.MapFrom(s => s.Material.PriceHistories
                    .Where(p => p.IsCurrent)
                    .Select(p => (DateTimeOffset?)p.LastModified)
                    .FirstOrDefault()))
                // Bug fix: was (1 + MarkupPercentage) which multiplied by e.g. 16 instead of 1.15
                // Correct formula: UnitPrice * (1 + MarkupPercentage / 100)
                .ForMember(d => d.FinalPrice, opt => opt.MapFrom(s => s.UnitPrice * (1 + s.MarkupPercentage / 100m)))
                .ForMember(d => d.ConfirmedAt, opt => opt.MapFrom(s => s.IsConfirmed ? s.LastModified : (DateTimeOffset?)null));

        }
    }
}
