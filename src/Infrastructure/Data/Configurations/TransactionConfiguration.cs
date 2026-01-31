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
            .HasPrecision(18, 2) 
            .IsRequired();

        builder.Property(t => t.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(t => t.ExternalTransactionId)
            .HasMaxLength(100);

        builder.Property(t => t.TransactionStatus)
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue("PENDING");

        builder.Property(t => t.Note)
            .HasMaxLength(500);

        // 4. Cấu hình quan hệ (Relationship)
        builder.HasOne(t => t.Invoice)
            .WithMany(i => i.Transactions)
            .HasForeignKey(t => t.InvoiceId)
            .IsRequired(); // ko xóa lịch sử giao dịch chung invoice

        // 5. Thêm Index cho các trường hay tìm kiếm
        builder.HasIndex(t => t.ExternalTransactionId);
    }
}
