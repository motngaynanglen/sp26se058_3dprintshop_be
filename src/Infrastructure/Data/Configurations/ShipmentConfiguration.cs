using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {

        builder.Property(s => s.TrackingNumber)
                 .HasMaxLength(100); // Mã vận đơn thường không quá dài

        builder.Property(s => s.ShippingFee)
            .HasPrecision(18, 2) // Chuẩn cho kiểu decimal trong tài chính
            .HasDefaultValue(0);

        builder.Property(s => s.ShipmentStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("PENDING");

        // Cấu hình mốc thời gian
        builder.Property(s => s.EstimatedDeliveryTime).IsRequired(false);
        builder.Property(s => s.ShippedAt).IsRequired(false);
        builder.Property(s => s.DeliveredAt).IsRequired(false);

        // --- CẤU HÌNH QUAN HỆ (RELATIONSHIPS) ---

        // 1. Shipment -> Order (1-1 hoặc N-1 tùy logic của bạn, thường là 1 đơn có 1 lần giao)
        builder.HasOne(s => s.Order)
            .WithMany(o => o.Shipments) 
            .HasForeignKey(s => s.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // 2. Shipment -> ShippingAddress
        builder.HasOne(s => s.ShippingAddress)
            .WithMany()
            .HasForeignKey(s => s.ShippingAddressId)
            .OnDelete(DeleteBehavior.Restrict); // Tránh xóa địa chỉ làm mất lịch sử shipment

        // 3. Shipment -> ShippingMethod (Đơn vị vận chuyển: GHTK, GHN,...)
        //builder.HasOne(s => s.ShippingMethod)
        //    .WithMany()
        //    .HasForeignKey(s => s.ShippingMethodId)
        //    .OnDelete(DeleteBehavior.Restrict);
    }
}
