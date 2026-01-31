using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class DesignVersionHistoryConfiguration : IEntityTypeConfiguration<DesignVersionHistory>
{
    public void Configure(EntityTypeBuilder<DesignVersionHistory> builder)
    {
        // Quan hệ với DesignWork: Bắt buộc
        builder.HasOne(dvh => dvh.DesignWork)
               .WithMany(dw => dw.VersionHistories)
               .HasForeignKey(dvh => dvh.DesignWorkId)
               .IsRequired()
               .OnDelete(DeleteBehavior.NoAction);

        //// Quan hệ với DesignLog: Tùy chọn (Nullable)
        //builder.HasOne(dvh => dvh.DesignLog)
        //       .WithMany(dl => dl.VersionHistories)
        //       .HasForeignKey(dvh => dvh.DesignLogId)
        //       .IsRequired(false) // Xác định rõ ràng không bắt buộc
        //       .OnDelete(DeleteBehavior.SetNull); // Nếu xóa Log, Version vẫn còn

        builder.HasOne(dvh => dvh.Uploader)
               .WithMany()
               .HasForeignKey(dvh => dvh.UploaderId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
