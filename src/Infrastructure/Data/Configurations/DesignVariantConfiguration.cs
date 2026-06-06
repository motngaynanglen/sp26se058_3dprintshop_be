using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class DesignVariantConfiguration : IEntityTypeConfiguration<DesignVariant>
{
    public void Configure(EntityTypeBuilder<DesignVariant> builder)
    {

        // Unique Constraint cho Code
        builder.HasIndex(v => v.Code).IsUnique();

        // Cấu hình các chuỗi nvarchar
        builder.Property(v => v.Name).IsRequired().HasMaxLength(255);
        builder.Property(v => v.Code).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Description).HasColumnType("nvarchar(max)");

        // Cấu hình Precision cho Decimal (Rất quan trọng cho in 3D và tiền tệ)
        builder.Property(v => v.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(v => v.MarkupPercentage).HasPrecision(5, 2).HasDefaultValue(0);
        builder.Property(v => v.SizeScale).HasPrecision(18, 2);
        builder.Property(v => v.EstimatedWeightPerUnit).HasPrecision(18, 4); // Gram nên cần độ chính xác cao
        builder.Property(v => v.EstimatedPrintTimePerUnit).HasPrecision(18, 2);

        // Default values
        builder.Property(v => v.StockQuantity).HasDefaultValue(0).IsRequired();
        builder.Property(v => v.MinimumStockLevel);
        builder.Property(v => v.IsAllowPreOrder).HasDefaultValue(true).IsRequired();
        builder.Property(v => v.IsActive).HasDefaultValue(true).IsRequired();

        // Relationships (Quan hệ ngoại)
        builder.HasOne(v => v.DesignTemplate)
               .WithMany(t => t.Variants)
               .HasForeignKey(v => v.DesignTemplateId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(v => v.Material)
               .WithMany() // Nếu bảng Material không cần list Variants thì để trống
               .HasForeignKey(v => v.MaterialId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
