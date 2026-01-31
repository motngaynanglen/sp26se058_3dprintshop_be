using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class DesignVersionHistory : BaseAuditableEntity
{
    public string? Tilte {  get; set; } = string.Empty;
    public Guid DesignWorkId { get; set; }

    public Guid? DesignLogId { get; set; }
    public Guid UploaderId { get; set; }

    public required string FileUrl { get; set; }
    public int VersionNumber { get; set; }
    public bool IsPreviewable { get; set; } = true;
    public bool IsPrintable { get; set; } = false;

    public virtual DesignLog? DesignLog { get; set; }
    public virtual Account Uploader { get; set; } = null!;
    public virtual DesignWork DesignWork { get; set; } = null!;
}
