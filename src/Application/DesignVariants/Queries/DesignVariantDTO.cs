using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

public class DesignVariantDTO
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? PreviewModelUrl { get; set; }
    public Guid DesignTemplateId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal? SizeScale { get; set; }
    public int StockQuantity { get; set; }
    public int? MinimumStockLevel { get; set; }
    public bool IsAllowPreOrder { get; set; } = false;
    public decimal? EstimatedWeightPerUnit { get; set; }
    public decimal? EstimatedPrintTimePerUnit { get; set; }
    public decimal MarkupPercentage { get; set; } = 0;
    public bool IsActive { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.DesignVariant, DesignVariantDTO>();
        }
        
    }
    
}
