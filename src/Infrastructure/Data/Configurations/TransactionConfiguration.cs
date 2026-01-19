using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2) // Quan trọng cho tiền tệ
            .IsRequired();

        builder.Property(t => t.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(t => t.ExternalTransactionId)
            .HasMaxLength(100);

        builder.Property(t => t.TransactionStatus)
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("PENDING");

        builder.Property(t => t.Note)
            .HasMaxLength(500);

        // 4. Cấu hình quan hệ (Relationship)
        builder.HasOne(t => t.Invoice)
            .WithMany(i => i.Transactions)
            .HasForeignKey(t => t.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa Invoice thì xóa lịch sử giao dịch

        // 5. Thêm Index cho các trường hay tìm kiếm (Senior Tip)
        builder.HasIndex(t => t.ExternalTransactionId);
    }
}
