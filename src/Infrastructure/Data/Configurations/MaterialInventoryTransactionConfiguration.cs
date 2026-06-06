using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class MaterialInventoryTransactionConfiguration : IEntityTypeConfiguration<MaterialInventoryTransaction>
{
    public void Configure(EntityTypeBuilder<MaterialInventoryTransaction> builder)
    {
        builder.Property(t => t.Type).IsRequired().HasMaxLength(50);
        builder.Property(t => t.QuantityGrams).HasPrecision(18, 4);
        builder.Property(t => t.Note).HasMaxLength(1000);

        builder.HasOne(t => t.Material)
            .WithMany(m => m.InventoryTransactions)
            .HasForeignKey(t => t.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Staff)
            .WithMany()
            .HasForeignKey(t => t.StaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.ReferenceId);
        builder.HasIndex(t => new { t.MaterialId, t.Created });
    }
}
