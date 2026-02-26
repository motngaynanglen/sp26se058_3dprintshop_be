using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class DesignTagConfiguration : IEntityTypeConfiguration<DesignTag>
{
    public void Configure(EntityTypeBuilder<DesignTag> builder)
    {

        builder.HasOne(dt => dt.ConceptTag)
               .WithMany(t => t.DesignTags)
               .HasForeignKey(dt => dt.ConceptTagId)
               .IsRequired();

        builder.HasOne(dt => dt.DesignTemplate)
               .WithMany(t => t.DesignTags)
               .HasForeignKey(dt => dt.DesignTemplateId)
               .IsRequired();
    }
}
