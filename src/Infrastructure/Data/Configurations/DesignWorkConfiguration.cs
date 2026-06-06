using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class DesignWorkConfiguration : IEntityTypeConfiguration<DesignWork>
{
    public void Configure(EntityTypeBuilder<DesignWork> builder)
    {
        builder.Property(dw => dw.Status).IsRequired().HasMaxLength(30);
        builder.Property(dw => dw.SourceType).IsRequired().HasMaxLength(50);
        builder.Property(dw => dw.LatestQuotedPrice).HasPrecision(18, 2);

        builder.HasOne(dw => dw.Customer)
               .WithMany(c => c.DesignWorks)
               .HasForeignKey(dw => dw.CustomerId)
               .IsRequired();

        builder.HasOne(dw => dw.MainAssignedStaff)
               .WithMany(s => s.AssignedDesignWorks)
               .HasForeignKey(dw => dw.MainAssignedStaffId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<DesignWork>()
               .WithMany()
               .HasForeignKey(dw => dw.SourceDesignWorkId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(dw => dw.ServicePackage)
               .WithMany()
               .HasForeignKey(dw => dw.ServicePackageId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
