using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class DesignTemplateConfiguration : IEntityTypeConfiguration<DesignTemplate>
{
    public void Configure(EntityTypeBuilder<DesignTemplate> builder)
    {
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(255);
        builder.Property(t => t.FileUrl).IsRequired();
        builder.Property(t => t.CatalogStatus).IsRequired().HasMaxLength(30).HasDefaultValue(Domain.Constants.Statuses.CatalogStatuses.Published);
        builder.HasIndex(t => t.CatalogStatus);
    }
}
