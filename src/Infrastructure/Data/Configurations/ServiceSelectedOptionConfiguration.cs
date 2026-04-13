using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class ServiceSelectedOptionConfiguration : IEntityTypeConfiguration<ServiceSelectedOption>
{
    public void Configure(EntityTypeBuilder<ServiceSelectedOption> builder)
    {

        // Quan hệ với bảng Selection chính
        builder.HasOne(x => x.ServiceSelection)
            .WithMany(s => s.ServiceSelectedOptions)
            .HasForeignKey(x => x.ServiceSelectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quan hệ với Option danh mục
        builder.HasOne(x => x.ServiceOption)
            .WithMany(so => so.ServiceSelectedOptions)
            .HasForeignKey(x => x.ServiceOptionId) 
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.AppliedPrice)
            .HasPrecision(18, 2)
            .IsRequired(); // Bắt buộc phải lưu giá tại thời điểm khách chọn

        builder.Property(x => x.Quantity)
            .HasDefaultValue(1);
    }
}
