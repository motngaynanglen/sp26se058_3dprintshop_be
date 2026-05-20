namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

public class DashboardChartPointDTO
{
    public string Key { get; init; } = null!;
    public string Label { get; init; } = null!;
    public decimal Value { get; init; }
    public int Count { get; init; }
}

public class DashboardChartSeriesDTO
{
    public string ChartKey { get; init; } = null!;
    public string Title { get; init; } = null!;
    public DateTimeOffset GeneratedAt { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? GroupBy { get; init; }
    public IReadOnlyCollection<DashboardChartPointDTO> Points { get; init; } = [];
}
