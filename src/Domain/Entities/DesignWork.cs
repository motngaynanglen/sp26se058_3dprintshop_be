using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class DesignWork : BaseAuditableEntity
{
    public string? Name { get; set; } = string.Empty;
    public required string SourceType { get; set; } // 'FromTemplate' hoặc 'NewConcept'

    public Guid? TemplateId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? MainAssignedStaffId { get; set; }

    /// <summary>DesignWork nguồn (TH2 in từ thiết kế / TH3 in lại).</summary>
    public Guid? SourceDesignWorkId { get; set; }

    public Guid? ServiceSelectionId { get; set; }
    public Guid? ServicePackageId { get; set; }

    public string? BaseImageUrl { get; set; }
    public Guid? ResultDraftId { get; set; } // Sẽ trỏ tới TechnicalDraftId sau khi xong

    public required string Status { get; set; } // 'Pending', 'InProgress', etc.

    /// <summary>Nội dung mô tả / yêu cầu (Mainflow 2: báo giá theo yêu cầu).</summary>
    public string? RequirementBrief { get; set; }
    /// <summary>JSON mảng URL ảnh ý tưởng ban đầu (Mainflow 2).</summary>
    public string? InitialIdeaImageUrlsJson { get; set; }
    public decimal? LatestQuotedPrice { get; set; }
    public int QuoteRevision { get; set; }
    public DateTimeOffset? StaffAssignedAt { get; set; }
    public DateTimeOffset? LastQuotedAt { get; set; }
    public DateTimeOffset? CustomerApprovedAt { get; set; }

    //nativation
    public virtual ServiceSelection? ServiceSelection { get; set; }
    public virtual ServicePackage? ServicePackage { get; set; }
    public virtual Customer Customer { get; set; } = null!;
    public virtual Staff? MainAssignedStaff { get; set; }
    public virtual DesignTemplate? Template { get; set; }
    public virtual ICollection<DesignLog> DesignLogs { get; set; } = new List<DesignLog>();
    public virtual ICollection<DesignVersionHistory> VersionHistories { get; set; } = new List<DesignVersionHistory>();
}
