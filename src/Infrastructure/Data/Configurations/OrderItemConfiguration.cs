using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(oi => oi.QuantityOrdered).IsRequired();
        builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(oi => oi.TotalPrice).HasPrecision(18, 2).IsRequired();
        //builder.Property(oi => oi.TotalServiceCostPerGram).HasPrecision(18, 4);
        builder.Property(oi => oi.SourceType).IsRequired().HasMaxLength(50);
        builder.Property(oi => oi.FulfillmentStatus).IsRequired().HasMaxLength(50);

        builder.HasOne(oi => oi.Order)
               .WithMany(o => o.OrderItems)
               .HasForeignKey(oi => oi.OrderId)
               .IsRequired();
    }
}
