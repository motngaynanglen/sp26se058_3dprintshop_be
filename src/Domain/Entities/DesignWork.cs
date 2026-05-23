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
    
    // === SELF-REFERENCING & BRANCHING ===
    public Guid? ParentDesignWorkId { get; set; }
    public Guid RootDesignWorkId { get; set; }
    public string RelationshipType { get; set; } = "ORIGINAL"; // ORIGINAL, REVISION, BRANCH, CLONE


    // === BUSINESS FIELDS ===
    public Guid CustomerId { get; set; }
    public Guid? MainAssignedStaffId { get; set; }
    public string? BaseImageUrl { get; set; }
    public Guid? ResultDraftId { get; set; }

    public string Status { get; set; } = "SKETCHING";
    public bool IsLocked { get; set; } = false; // Khóa khi Completed

    /// <summary>
    /// Distinguishes the two DesignWork flows.
    /// null = legacy data (treated as DESIGN_SERVICE).
    /// Set by CheckoutDesignWorkCommand ("DESIGN_SERVICE") and CheckoutQuickPrintCommand ("PRINT_SERVICE").
    /// See DesignWorkTypes constants.
    /// </summary>
    public string? WorkType { get; set; }

    // === NAVIGATION PROPERTIES ===
    public virtual DesignWork? ParentDesignWork { get; set; }
    public virtual ICollection<DesignWork> ChildDesignWorks { get; set; } = new List<DesignWork>();

    public virtual Customer Customer { get; set; } = null!;
    public virtual Staff? MainAssignedStaff { get; set; }
    public virtual ICollection<ServiceSelection> ServiceSelections { get; set; } = new List<ServiceSelection>();
    public virtual ICollection<DesignLog> DesignLogs { get; set; } = new List<DesignLog>();
    public virtual ICollection<DesignVersionHistory> VersionHistories { get; set; } = new List<DesignVersionHistory>();
}
