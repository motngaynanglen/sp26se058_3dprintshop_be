using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class DesignWorkConfiguration : IEntityTypeConfiguration<DesignWork>
{
    public void Configure(EntityTypeBuilder<DesignWork> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(255);
        builder.Property(x => x.Status).HasDefaultValue(DesignWorkStatus.Sketching).HasMaxLength(30).IsRequired();
        builder.Property(x => x.RelationshipType).HasDefaultValue(DesignRelationshipType.Original).HasMaxLength(30).IsRequired();

        builder.HasOne(x => x.ParentDesignWork)
            .WithMany(x => x.ChildDesignWorks)
            .HasForeignKey(x => x.ParentDesignWorkId)
            .OnDelete(DeleteBehavior.Restrict); // Không xóa dây chuyền để giữ lịch sử

        builder.HasOne(dw => dw.Customer)
               .WithMany(c => c.DesignWorks)
               .HasForeignKey(dw => dw.CustomerId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        builder.HasOne(x => x.MainAssignedStaff)
            .WithMany()
            .HasForeignKey(x => x.MainAssignedStaffId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.RootDesignWorkId);
    }
}
