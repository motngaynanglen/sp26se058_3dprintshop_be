using sp26se058_3dprintshop_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.HasIndex(s => s.AccountId).IsUnique();

        builder.HasOne(s => s.Account)
               .WithOne(a => a.Staff)
               .HasForeignKey<Staff>(s => s.AccountId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);
    }
}
