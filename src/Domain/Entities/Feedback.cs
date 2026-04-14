using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class Feedback : BaseAuditableEntity
{
    public Guid CustomerId { get; set; }
    public Guid DesignTemplateId { get; set; }
    public Guid OrderItemId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? StaffReply { get; set; }
    public DateTimeOffset? RepliedDate { get; set; }

    public bool IsHidden { get; set; } = false;

    // Navigation Properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual DesignTemplate DesignTemplate { get; set; } = null!;
    public virtual OrderItem OrderItem { get; set; } = null!;
    public virtual ICollection<FeedbackImage> FeedbackImages { get; set; } = new List<FeedbackImage>();
}
