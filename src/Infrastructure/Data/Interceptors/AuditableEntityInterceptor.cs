using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Infrastructure.Data.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IUser _user;
    //Phương thức này đang gặp vấn đề nên ae gắn thủ công nha
    public AuditableEntityInterceptor(IUser user)
    {
        _user = user;
    }

    // Ghi đè phương thức đồng bộ
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    // Ghi đè phương thức bất đồng bộ 
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null) return;
        var utcNow = CoreHelper.SystemTimeNow;
        var user = GetCurrentUsername();

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {


            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = user;
                entry.Entity.Created = utcNow;

                // Khi thêm mới thì LastModified cũng khởi tạo luôn
                entry.Entity.LastModifiedBy = user;
                entry.Entity.LastModified = utcNow;
            }
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.LastModifiedBy = user;
                entry.Entity.LastModified = utcNow;
            }
            if (entry.State == EntityState.Deleted)
            {
                // Logic Soft Delete của Bách
                if (entry.Entity.DeletedBy == "HARD_DELETE_FLAG")
                {
                    continue;
                }

                // Chuyển từ Delete sang Modified để Update thay vì Delete thật trong DB
                entry.State = EntityState.Modified;

                entry.Entity.DeletedBy = user;
                entry.Entity.Deleted = utcNow;

                // Cập nhật luôn dấu vết sửa đổi cuối cùng
                entry.Entity.LastModifiedBy = user;
                entry.Entity.LastModified = utcNow;
            }

        }
    }
    private string GetCurrentUsername()
    {
        // 1. Nếu không có User (chưa đăng nhập - ví dụ: Register)
        if (string.IsNullOrEmpty(_user.Username))
        {
            return "SYSTEM";
        }
        return _user.Username;
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r => 
            r.TargetEntry != null && 
            r.TargetEntry.Metadata.IsOwned() && 
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}
