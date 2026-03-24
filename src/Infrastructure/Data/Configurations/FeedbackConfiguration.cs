using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Rating)
            .IsRequired();

        builder.Property(f => f.Comment)
            .HasMaxLength(2000);

        builder.Property(f => f.StaffReply)
            .HasMaxLength(2000);

        // Mỗi OrderItem chỉ được feedback một lần duy nhất
        builder.HasIndex(f => f.OrderItemId).IsUnique();

        // Relationships
        builder.HasOne(f => f.Customer)
            .WithMany() // Nếu Customer không có List<Feedback>
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.DesignTemplate)
            .WithMany()
            .HasForeignKey(f => f.DesignTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        
    }
}
