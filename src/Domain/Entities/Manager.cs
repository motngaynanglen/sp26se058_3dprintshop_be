using System.ComponentModel.DataAnnotations.Schema;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class Manager : BaseAuditableEntity
{
    public Guid AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;
}
