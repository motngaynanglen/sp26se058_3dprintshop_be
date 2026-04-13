using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class ServiceSelectionConfiguration : IEntityTypeConfiguration<ServiceSelection>
{
    public void Configure(EntityTypeBuilder<ServiceSelection> builder)
    {
        // One-to-One giữa DesignWork và Selection
        builder.HasOne(x => x.DesignWork)
            .WithMany(x => x.ServiceSelections)
            .HasForeignKey(x => x.DesignWorkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ServicePackage)
            .WithMany()
            .HasForeignKey(x => x.ServicePackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
