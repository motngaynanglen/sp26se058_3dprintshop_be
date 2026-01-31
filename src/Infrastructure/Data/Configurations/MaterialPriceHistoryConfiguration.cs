using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class MaterialPriceHistoryConfiguration : IEntityTypeConfiguration<MaterialPriceHistory>
{
    public void Configure(EntityTypeBuilder<MaterialPriceHistory> builder)
    {
        builder.Property(p => p.BaseCostPerGram).HasPrecision(18, 4);
        builder.Property(p => p.TotalServiceCostPerGram).HasPrecision(18, 4);

        builder.HasOne(p => p.Material)
               .WithMany(m => m.PriceHistories)
               .HasForeignKey(p => p.MaterialId)
               .IsRequired();
    }
}
