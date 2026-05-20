using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class ShipmentAddressChangeRequestConfiguration : IEntityTypeConfiguration<ShipmentAddressChangeRequest>
{
    public void Configure(EntityTypeBuilder<ShipmentAddressChangeRequest> builder)
    {
        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue(ShipmentAddressChangeRequestStatuses.Pending);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ResponseNote)
            .HasMaxLength(500);

        builder.HasOne(x => x.Shipment)
            .WithMany(x => x.AddressChangeRequests)
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RequestedByCustomer)
            .WithMany()
            .HasForeignKey(x => x.RequestedByCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.NewShippingAddress)
            .WithMany()
            .HasForeignKey(x => x.NewShippingAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByAccount)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
