using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class DesignVersionHistory : BaseAuditableEntity
{
    public string? Tilte { get; set; } = string.Empty;
    public Guid DesignWorkId { get; set; }

    public Guid? DesignLogId { get; set; }
    public Guid UploaderId { get; set; }

    public required string FileUrl { get; set; }
    public int VersionNumber { get; set; }
    public bool IsPreviewable { get; set; } = true;
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// Technical file quality status — PENDING / ACCEPTED / REJECTED.
    /// Shared by Design Service (staff marks design file printable) and POD (staff reviews
    /// customer-submitted STL/OBJ). Set by ReviewFileVersionCommand.
    /// </summary>
    public string FileReviewStatus { get; set; } = FileReviewStatuses.Pending;

    /// <summary>
    /// Rejection reason or fix instructions written by staff.
    /// Required when FileReviewStatus == REJECTED.
    /// </summary>
    public string? FileReviewNote { get; set; }

    /// <summary>
    /// Legacy boolean kept for backward compatibility.
    /// Semantically equivalent to FileReviewStatus == ACCEPTED.
    /// Set automatically by ReviewFileVersionCommand — do not set manually.
    /// </summary>
    public bool IsPrintable { get; set; } = false;

    public virtual DesignLog? DesignLog { get; set; }
    public virtual Account Uploader { get; set; } = null!;
    public virtual DesignWork DesignWork { get; set; } = null!;
}
