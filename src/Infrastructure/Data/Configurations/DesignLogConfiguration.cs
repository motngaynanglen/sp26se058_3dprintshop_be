using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class DesignLogConfiguration : IEntityTypeConfiguration<DesignLog>
{
    public void Configure(EntityTypeBuilder<DesignLog> builder)
    {
        builder.Property(x => x.LogType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Content).IsRequired(false);

        // Cấu hình Threading (Self-referencing)
        builder.HasOne(x => x.ParentLog)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ParentLogId)
            .OnDelete(DeleteBehavior.Restrict);

        // Liên kết với DesignWork
        builder.HasOne(x => x.DesignWork)
            .WithMany(x => x.DesignLogs)
            .HasForeignKey(x => x.DesignWorkId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // === INDEXING ===
        // Luôn tìm log theo dự án và sắp xếp theo thời gian
        builder.HasIndex(x => new { x.DesignWorkId, x.Created });
    }
}
