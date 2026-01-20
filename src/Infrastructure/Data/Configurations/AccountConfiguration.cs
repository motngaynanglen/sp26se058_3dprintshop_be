using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;
using System.Security.Cryptography;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        // 1. Cấu hình các ràng buộc dữ liệu
        builder.Property(a => a.Username)
         .HasMaxLength(20)
         .IsRequired();

        builder.Property(a => a.Fullname)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(a => a.Email)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(a => a.Profile_Image_URL)
            .HasMaxLength(255);

        builder.Property(a => a.Contact_Phone)
            .HasMaxLength(15);

        builder.Property(a => a.Zalo_Phone)
            .HasMaxLength(15);

        builder.Property(a => a.Password_Hash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.Is_active)
            .HasDefaultValue(true);

        // 2. Đánh Index Unique cho Username và Email (Tránh trùng lặp)
        builder.HasIndex(a => a.Username).IsUnique();
        builder.HasIndex(a => a.Email).IsUnique();

        // 3. Cấu hình quan hệ 1:1 với các bảng Role
        // Khi xóa Account, tự động xóa luôn bản ghi chi tiết ở bảng Role
        builder.HasOne(a => a.Staff)
            .WithOne(s => s.Account)
            .HasForeignKey<Staff>(s => s.AccountId) // Giả định bảng Staff có AccountId
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Customer)
            .WithOne(c => c.Account)
            .HasForeignKey<Customer>(c => c.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Manager)
            .WithOne(m => m.Account)
            .HasForeignKey<Manager>(m => m.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
