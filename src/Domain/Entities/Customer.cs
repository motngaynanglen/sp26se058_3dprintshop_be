using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp26se058_3dprintshop_be.Domain.Entities;

public class Customer : BaseAuditableEntity
{

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    public Guid AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    public Account Account { get; set; } = null!;
}
