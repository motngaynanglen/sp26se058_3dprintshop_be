using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // Cấu hình thuộc tính (Thay cho các Tag)
        builder.Property(o => o.CustomerId)
            .IsRequired(); // Tương đương [Required]

        builder.Property(o => o.TotalPrice)
            .HasPrecision(18, 2);

        builder.Property(o => o.OrderStatus)
            .HasMaxLength(20) // Tương đương [MaxLength(20)]
            .HasDefaultValue("PENDING")
            .IsRequired();
        // Cấu hình quan hệ 1-N vs customer
        builder.HasOne(o => o.Customer)       // Một đơn hàng có một khách hàng
            .WithMany(a => a.Orders)          // Một khách hàng có nhiều đơn hàng
            .HasForeignKey(o => o.CustomerId) // Khóa ngoại là CustomerId
            .OnDelete(DeleteBehavior.Restrict); // Không xóa khách hàng nếu đã có đơn hàng (để bảo vệ dữ liệu audit)

        // Cấu hình quan hệ 1-N với OrderItems
        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa Order thì xóa luôn OrderItems

        // Cấu hình quan hệ 1-1 với Invoice
        // Lưu ý: Invoice thường giữ Foreign Key trỏ về Order
        builder.HasOne(o => o.Invoice)
            .WithOne(i => i.Order)
            .HasForeignKey<Invoice>(i => i.OrderId);
    }
}
