using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    private const string UnknownSnapshotValue = "N/A";

    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.Property(s => s.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(s => s.ShippingFee)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(s => s.ShipmentStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(ShipmentStatuses.Preparing);

        builder.Property(s => s.RecipientName)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue(UnknownSnapshotValue);

        builder.Property(s => s.RecipientPhone)
            .IsRequired()
            .HasMaxLength(15)
            .HasDefaultValue(UnknownSnapshotValue);

        builder.Property(s => s.AddressLine)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue(UnknownSnapshotValue);

        builder.Property(s => s.Ward)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue(UnknownSnapshotValue);

        builder.Property(s => s.District)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue(UnknownSnapshotValue);

        builder.Property(s => s.City)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue(UnknownSnapshotValue);

        builder.Property(s => s.Province)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue(UnknownSnapshotValue);

        builder.Property(s => s.Note)
            .IsRequired(false);

        builder.Property(s => s.EstimatedDeliveryTime).IsRequired(false);
        builder.Property(s => s.ShippedAt).IsRequired(false);
        builder.Property(s => s.DeliveredAt).IsRequired(false);

        builder.HasOne(s => s.Order)
            .WithMany(o => o.Shipments)
            .HasForeignKey(s => s.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.ShippingAddress)
            .WithMany()
            .HasForeignKey(s => s.ShippingAddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
