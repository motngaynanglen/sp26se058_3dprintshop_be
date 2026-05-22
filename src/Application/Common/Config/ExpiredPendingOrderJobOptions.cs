namespace sp26se058_3dprintshop_be.Application.Common.Config;

public class ExpiredPendingOrderJobOptions
{
    public const string SectionName = "ExpiredPendingOrderJob";

    public int IntervalMinutes { get; init; } = 15;
    public int BatchSize { get; init; } = 50;
}
