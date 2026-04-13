using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class DesignWorkConfiguration : IEntityTypeConfiguration<DesignWork>
{
    public void Configure(EntityTypeBuilder<DesignWork> builder)
    {
        builder.Property(dw => dw.Status).IsRequired().HasMaxLength(30);

        builder.HasOne(dw => dw.Customer)
               .WithMany(c => c.DesignWorks)
               .HasForeignKey(dw => dw.CustomerId)
               .IsRequired();

        builder.HasOne(dw => dw.MainAssignedStaff)
               .WithMany(s => s.AssignedDesignWorks)
               .HasForeignKey(dw => dw.MainAssignedStaffId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
