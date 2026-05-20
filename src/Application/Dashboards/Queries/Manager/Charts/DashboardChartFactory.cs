using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

internal static class DashboardChartFactory
{
    public static DashboardChartSeriesDTO Create(string key, string title, IReadOnlyCollection<DashboardChartPointDTO> points)
    {
        return new DashboardChartSeriesDTO
        {
            ChartKey = key,
            Title = title,
            GeneratedAt = CoreHelper.SystemTimeNow,
            Points = points
        };
    }
}
