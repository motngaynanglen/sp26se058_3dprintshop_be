using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerRevenueChartQuery : IRequest<DashboardChartSeriesDTO>
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string GroupBy { get; init; } = "day";
}

public class GetManagerRevenueChartQueryHandler : IRequestHandler<GetManagerRevenueChartQuery, DashboardChartSeriesDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerRevenueChartQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardChartSeriesDTO> Handle(GetManagerRevenueChartQuery request, CancellationToken ct)
    {
        var from = request.From ?? CoreHelper.SystemTimeNow.UtcDateTime.Date.AddDays(-29);
        var to = request.To ?? CoreHelper.SystemTimeNow.UtcDateTime.Date;
        var inclusiveTo = to.Date.AddDays(1);
        var groupBy = request.GroupBy.Equals("month", StringComparison.OrdinalIgnoreCase) ? "month" : "day";

        var paidInvoices = _context.Invoices
            .Where(x => x.PaymentStatus == InvoiceStatuses.Paid
                && x.Created.UtcDateTime >= from.Date
                && x.Created.UtcDateTime < inclusiveTo);

        // Gom nhóm + Sum/Count trong SQL, format chuỗi Key/Label ở memory
        // (EF/MySQL không dịch được DateTime/int.ToString(format)).
        List<DashboardChartPointDTO> points;
        if (groupBy == "month")
        {
            var raw = await paidInvoices
                .GroupBy(x => new { x.Created.Year, x.Created.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Value = g.Sum(i => i.TotalAmount), Count = g.Count() })
                .ToListAsync(ct);

            points = raw
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .Select(r => new DashboardChartPointDTO
                {
                    Key = $"{r.Year}-{r.Month:00}",
                    Label = $"{r.Month:00}/{r.Year}",
                    Value = r.Value,
                    Count = r.Count
                })
                .ToList();
        }
        else
        {
            var raw = await paidInvoices
                .GroupBy(x => x.Created.Date)
                .Select(g => new { Day = g.Key, Value = g.Sum(i => i.TotalAmount), Count = g.Count() })
                .ToListAsync(ct);

            points = raw
                .OrderBy(r => r.Day)
                .Select(r => new DashboardChartPointDTO
                {
                    Key = r.Day.ToString("yyyy-MM-dd"),
                    Label = r.Day.ToString("dd/MM"),
                    Value = r.Value,
                    Count = r.Count
                })
                .ToList();
        }

        return new DashboardChartSeriesDTO
        {
            ChartKey = "revenue",
            Title = "Doanh thu đã thanh toán",
            GeneratedAt = CoreHelper.SystemTimeNow,
            From = from.Date,
            To = to.Date,
            GroupBy = groupBy,
            Points = points
        };
    }
}
