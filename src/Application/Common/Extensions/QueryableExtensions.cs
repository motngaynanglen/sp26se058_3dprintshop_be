using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Common;

namespace sp26se058_3dprintshop_be.Application.Common.Extensions;
public static class QueryableExtensions
{
    public static async Task<DuplicateCheckResult> GetDuplicateResultAsync<TEntity>(
        this IQueryable<TEntity> source,
        Expression<Func<TEntity, bool>> predicate,
        string entityName,
        string fieldName,
        object value,
        Guid? excludeId = null,
        CancellationToken ct = default) where TEntity : BaseAuditableEntity // Ràng buộc vào Base
    {
        // 1. Luôn check cả những thằng đã xóa mềm
        var query = source.AsNoTracking().IgnoreQueryFilters();

        // 2. Nếu là Update, loại trừ chính nó dựa vào Id của BaseEntity
        if (excludeId.HasValue && excludeId.Value != Guid.Empty)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        // 3. Tìm bản ghi trùng
        var duplicate = await query.FirstOrDefaultAsync(predicate, ct);

        return new DuplicateCheckResult
        {
            IsDuplicate = duplicate != null,
            // Bách dùng DateTimeOffset? Deleted nên check != null là xong
            IsDeleted = duplicate?.Deleted != null,
            EntityName = entityName,
            FieldName = fieldName,
            Value = value
        };
    }
}
