using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class ShippingAddressConfiguration : IEntityTypeConfiguration<ShippingAddress>
{
    public void Configure(EntityTypeBuilder<ShippingAddress> builder)
    {
        // Matches 20260322182151_AddShippingTable (table was created as singular; snapshot pluralized without a rename migration)
        builder.ToTable("ShippingAddress");

        // 3. Cấu hình các thuộc tính chuỗi (varchar)
        builder.Property(s => s.ReceiverName)
            .IsRequired()
            .HasMaxLength(255); // Giới hạn độ dài để tối ưu DB

        builder.Property(s => s.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.AddressLine)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Ward)
            .HasMaxLength(100);

        builder.Property(s => s.District)
            .HasMaxLength(100);

        builder.Property(s => s.City)
            .HasMaxLength(100);

        builder.Property(s => s.Province)
            .HasMaxLength(100);

        // 4. Các thuộc tính khác
        builder.Property(s => s.IsDefault)
            .HasDefaultValue(false);

        // 5. Cấu hình Quan hệ (Relationship)
        // Một Customer có nhiều ShippingAddresses, một ShippingAddress thuộc về một Customer
        builder.HasOne(s => s.Customer)
            .WithMany(c => c.ShippingAddresses) // Đảm bảo trong class Customer có: 
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa Customer thì xóa luôn địa chỉ
    }
}
