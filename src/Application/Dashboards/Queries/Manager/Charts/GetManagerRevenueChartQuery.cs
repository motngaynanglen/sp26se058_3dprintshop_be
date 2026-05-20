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

        var points = groupBy == "month"
            ? await paidInvoices
                .GroupBy(x => new { x.Created.Year, x.Created.Month })
                .Select(x => new DashboardChartPointDTO
                {
                    Key = x.Key.Year + "-" + x.Key.Month.ToString("00"),
                    Label = x.Key.Month.ToString("00") + "/" + x.Key.Year,
                    Value = x.Sum(i => i.TotalAmount),
                    Count = x.Count()
                })
                .OrderBy(x => x.Key)
                .ToListAsync(ct)
            : await paidInvoices
                .GroupBy(x => x.Created.Date)
                .Select(x => new DashboardChartPointDTO
                {
                    Key = x.Key.ToString("yyyy-MM-dd"),
                    Label = x.Key.ToString("dd/MM"),
                    Value = x.Sum(i => i.TotalAmount),
                    Count = x.Count()
                })
                .OrderBy(x => x.Key)
                .ToListAsync(ct);

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
