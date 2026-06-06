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
        builder.Property(a => a.Username).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Fullname).HasMaxLength(40);
        builder.Property(a => a.Email).HasMaxLength(40);
        builder.Property(a => a.PasswordHash).IsRequired().HasMaxLength(255);


        builder.Property(a => a.ProfileImageURL).HasMaxLength(255);
        builder.Property(a => a.ContactPhone).HasMaxLength(15);
        builder.Property(a => a.ZaloPhone).HasMaxLength(15);

        builder.Property(a => a.IsActive).HasDefaultValue(true);

        // 2. Đánh Index Unique cho Username, Email và ContactPhone (Tránh trùng lặp)
        builder.HasIndex(a => a.Username).IsUnique();
        builder.HasIndex(a => a.Email).IsUnique();
        builder.HasIndex(a => a.ContactPhone).IsUnique();

        // 3. Cấu hình quan hệ 1:1 với các bảng Role
        // Khi xóa Account, tự động xóa luôn bản ghi chi tiết ở bảng Role
        builder.HasOne(a => a.Staff)
            .WithOne(s => s.Account)
            .HasForeignKey<Staff>(s => s.AccountId) // Giả định bảng Staff có AccountId
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Customer)
            .WithOne(c => c.Account)
            .HasForeignKey<Customer>(c => c.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Manager)
            .WithOne(m => m.Account)
            .HasForeignKey<Manager>(m => m.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
