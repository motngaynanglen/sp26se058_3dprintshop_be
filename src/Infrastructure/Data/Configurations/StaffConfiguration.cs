using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class ServicePackageConfiguration : IEntityTypeConfiguration<ServicePackage>
{
    public void Configure(EntityTypeBuilder<ServicePackage> builder)
    {
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.Code).IsRequired().HasMaxLength(20);
        builder.Property(s => s.BasePrice).HasPrecision(18, 2);
    }
}
