using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasIndex(i => i.InvoiceCode).IsUnique();
        builder.Property(i => i.InvoiceCode).IsRequired().HasMaxLength(50);

        builder.Property(i => i.SubTotal).HasPrecision(18, 2);
        builder.Property(i => i.ShippingFee).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);

        builder.Property(i => i.PaymentStatus).HasMaxLength(20).HasDefaultValue("UNPAID").IsRequired();

        // Cấu hình quan hệ 1-1 với Order
        builder.HasOne(i => i.Order)
            .WithOne(o => o.Invoice)
            .HasForeignKey<Invoice>(i => i.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

    }
}
