using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.Property(it => it.Type).IsRequired().HasMaxLength(30);

        builder.HasOne(it => it.DesignVariant)
               .WithMany(dv => dv.InventoryTransactions)
               .HasForeignKey(it => it.DesignVariantId)
               .IsRequired();

        builder.HasOne(it => it.Staff)
               .WithMany()
               .HasForeignKey(it => it.StaffId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
