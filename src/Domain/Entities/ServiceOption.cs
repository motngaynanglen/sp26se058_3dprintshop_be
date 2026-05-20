using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class ServiceOption : BaseAuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string GroupCode { get; set; } = "GENERAL";
    public string GroupName { get; set; } = "Chung";
    public string SelectionType { get; set; } = "ADDON";
    public decimal DefaultPrice { get; set; }   
    public int MinQuantity { get; set; } = 1;
    public int? MaxQuantity { get; set; }
    public int? AdjustmentRoundDelta { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public virtual ICollection<ServiceSelectedOption> ServiceSelectedOptions { get; set; } = new List<ServiceSelectedOption>();

}
