using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class ServiceOptionConfiguration : IEntityTypeConfiguration<ServiceOption>
{
    public void Configure(EntityTypeBuilder<ServiceOption> builder)
    {

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Code).IsUnique(); // Tránh trùng mã Option

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.GroupCode)
            .HasMaxLength(50)
            .HasDefaultValue("GENERAL")
            .IsRequired();

        builder.Property(x => x.GroupName)
            .HasMaxLength(100)
            .HasDefaultValue("Chung")
            .IsRequired();

        builder.Property(x => x.SelectionType)
            .HasMaxLength(30)
            .HasDefaultValue("ADDON")
            .IsRequired();

        builder.Property(x => x.DefaultPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.MinQuantity)
            .HasDefaultValue(1);

        builder.Property(x => x.SortOrder)
            .HasDefaultValue(0);

        
    }
}
