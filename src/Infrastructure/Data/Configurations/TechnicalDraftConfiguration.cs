using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class TechnicalDraftConfiguration : IEntityTypeConfiguration<TechnicalDraft>
{
    public void Configure(EntityTypeBuilder<TechnicalDraft> builder)
    {
        builder.Property(td => td.Price).HasPrecision(18, 2);

        builder.Property(td => td.LayerHeight).HasPrecision(18, 4);
        builder.Property(td => td.EstimatedWeightPerUnit).HasPrecision(18, 4);
        builder.Property(td => td.EstimatedPrintTimePerUnit).HasPrecision(18, 2);
        builder.Property(td => td.MarkupPercentage).HasPrecision(18, 2);

        builder.HasOne(td => td.DesignVersionHistory)
               .WithMany() // Một version có thể có nhiều phương án in nháp khác nhau
               .HasForeignKey(td => td.DesignVersionHistoryId)
               .IsRequired();

        builder.HasOne(td => td.Material)
               .WithMany()
               .HasForeignKey(td => td.MaterialId)
               .IsRequired();
    }
}
