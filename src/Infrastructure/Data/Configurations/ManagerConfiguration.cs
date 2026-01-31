using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class ManagerConfiguration : IEntityTypeConfiguration<Manager>
{
    public void Configure(EntityTypeBuilder<Manager> builder)
    {
        builder.HasIndex(m => m.AccountId).IsUnique();

        builder.HasOne(m => m.Account)
               .WithOne(a => a.Manager)
               .HasForeignKey<Manager>(m => m.AccountId)
               .IsRequired()
               .OnDelete(DeleteBehavior.NoAction);
    }
}
