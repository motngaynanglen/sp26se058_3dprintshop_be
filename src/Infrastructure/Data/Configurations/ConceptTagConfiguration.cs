using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class ConceptTagConfiguration : IEntityTypeConfiguration<ConceptTag>
{
    public void Configure(EntityTypeBuilder<ConceptTag> builder)
    {
        builder.HasIndex(t => t.Name).IsUnique();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
    }
}
