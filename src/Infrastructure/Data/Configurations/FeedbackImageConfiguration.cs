using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;
public class FeedbackImageConfiguration : IEntityTypeConfiguration<FeedbackImage>
{
    public void Configure(EntityTypeBuilder<FeedbackImage> builder)
    {
        builder.HasKey(fi => fi.Id);
        builder.Property(fi => fi.ImageUrl).IsRequired().HasMaxLength(500);
        builder.HasOne(f => f.Feedback)
            .WithMany(fi => fi.FeedbackImages)
            .HasForeignKey(fi => fi.FeedbackId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
