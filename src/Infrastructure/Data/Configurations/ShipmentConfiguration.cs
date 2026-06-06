using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        // DB migration 20260322182151 tạo bảng singular "Shipment" (không phải Shipments)
        builder.ToTable("Shipment");

        builder.Property(s => s.ShipmentStatus)
            .HasMaxLength(50)
            .HasDefaultValue("PENDING");

        builder.Property(s => s.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(s => s.Carrier)
            .HasMaxLength(20);

        builder.Property(s => s.CarrierOrderCode)
            .HasMaxLength(100);

        builder.Property(s => s.CarrierStatus)
            .HasMaxLength(80);

        builder.Property(s => s.CarrierLabelUrl)
            .HasMaxLength(500);

        builder.Property(s => s.CarrierMetaJson)
            .HasColumnType("text");

        builder.HasOne(s => s.Order)
            .WithMany()
            .HasForeignKey(s => s.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ShippingAddress)
            .WithMany()
            .HasForeignKey(s => s.ShippingAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.OrderId);
        builder.HasIndex(s => s.ShippingAddressId);
        builder.HasIndex(s => s.Deleted);
    }
}
