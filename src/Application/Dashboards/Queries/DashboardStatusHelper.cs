using sp26se058_3dprintshop_be.Domain.Common;
using System.Linq.Expressions;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

internal static class DashboardStatusHelper
{
    public static async Task<IReadOnlyCollection<DashboardStatusCountDTO>> CountByStatusAsync<TEntity>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, string>> statusSelector,
        CancellationToken ct)
    {
        return await query
            .GroupBy(statusSelector)
            .Select(x => new DashboardStatusCountDTO
            {
                Status = x.Key,
                Label = string.Empty,
                Count = x.Count()
            })
            .ToListAsync(ct);
    }

    public static IReadOnlyCollection<DashboardStatusCountDTO> Merge(
        IEnumerable<StatusDefinition> definitions,
        IEnumerable<DashboardStatusCountDTO> counts)
    {
        var countMap = counts.ToDictionary(x => x.Status, x => x.Count, StringComparer.OrdinalIgnoreCase);

        return definitions
            .Select(x => new DashboardStatusCountDTO
            {
                Status = x.Value,
                Label = x.Label,
                Count = countMap.TryGetValue(x.Value, out var count) ? count : 0
            })
            .ToList();
    }
}
