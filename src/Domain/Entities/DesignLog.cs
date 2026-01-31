using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class DesignLog : BaseAuditableEntity
{
    public Guid DesignWorkId { get; set; }
    public bool IsAI { get; set; } = false;

    public Guid? AccountId { get; set; }
    public string? Content { get; set; }
    public string? Metadata { get; set; } // Lưu JSON: ["url1.jpg", "url2.jpg"]
    public required string LogType { get; set; }

    public virtual Account? Account { get; set; }
    public virtual DesignWork DesignWork { get; set; } = null!;
    public virtual ICollection<DesignVersionHistory> VersionHistories { get; set; } = new List<DesignVersionHistory>();
}
