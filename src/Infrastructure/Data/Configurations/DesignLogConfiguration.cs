using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class DesignLogConfiguration : IEntityTypeConfiguration<DesignLog>
{
    public void Configure(EntityTypeBuilder<DesignLog> builder)
    {
        builder.Property(dl => dl.LogType).IsRequired().HasMaxLength(30);

        builder.HasOne(dl => dl.DesignWork)
               .WithMany(dw => dw.DesignLogs)
               .HasForeignKey(dl => dl.DesignWorkId)
               .IsRequired();
    }
}
