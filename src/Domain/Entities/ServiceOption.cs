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
    public decimal DefaultPrice { get; set; }   
    public bool IsActive { get; set; } = true;
    public virtual ICollection<ServiceSelectedOption> ServiceSelectedOptions { get; set; } = new List<ServiceSelectedOption>();

}
