using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Entities;
public class FeedbackImage : BaseEntity
{
    public Guid FeedbackId { get; set; }
    public string ImageUrl { get; set; } = null!;

    // Navigation Property
    public virtual Feedback Feedback { get; set; } = null!;
}
