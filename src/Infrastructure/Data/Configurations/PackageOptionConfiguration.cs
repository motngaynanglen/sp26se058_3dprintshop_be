using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class PackageOptionConfiguration : IEntityTypeConfiguration<PackageOption>
{
    public void Configure(EntityTypeBuilder<PackageOption> builder)
    {

        // Cấu hình quan hệ N-N giữa Package và Option
        builder.HasOne(x => x.ServicePackage)
            .WithMany(p => p.PackageOptions)
            .HasForeignKey(x => x.ServicePackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ServiceOption)
            .WithMany(s => s.PackageOptions)
            .HasForeignKey(x => x.ServiceOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.PriceOverride)
            .HasPrecision(18, 2)
            .IsRequired(false);

        // Các ràng buộc về số lượng
        builder.Property(x => x.MinQuantity).HasDefaultValue(0);
        builder.Property(x => x.MaxQuantity).HasDefaultValue(1);
    }
}
