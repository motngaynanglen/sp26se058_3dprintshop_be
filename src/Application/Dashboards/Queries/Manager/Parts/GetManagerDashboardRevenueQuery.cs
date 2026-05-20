using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardRevenueQuery : IRequest<ManagerDashboardRevenueSummaryDTO>;

public class GetManagerDashboardRevenueQueryHandler : IRequestHandler<GetManagerDashboardRevenueQuery, ManagerDashboardRevenueSummaryDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardRevenueQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ManagerDashboardRevenueSummaryDTO> Handle(GetManagerDashboardRevenueQuery request, CancellationToken ct)
    {
        var now = CoreHelper.SystemTimeNow;
        var currentMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var paidRevenue = await _context.Invoices
            .Where(x => x.PaymentStatus == InvoiceStatuses.Paid)
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;

        var currentMonthPaidRevenue = await _context.Invoices
            .Where(x => x.PaymentStatus == InvoiceStatuses.Paid && x.Created >= currentMonthStart)
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;

        var unpaidAmount = await _context.Invoices
            .Where(x => x.PaymentStatus == InvoiceStatuses.Unpaid)
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;

        return new ManagerDashboardRevenueSummaryDTO
        {
            GeneratedAt = now,
            Revenue = new DashboardRevenueDTO
            {
                PaidRevenue = paidRevenue,
                CurrentMonthPaidRevenue = currentMonthPaidRevenue,
                UnpaidAmount = unpaidAmount,
                PaidInvoiceCount = await _context.Invoices.CountAsync(x => x.PaymentStatus == InvoiceStatuses.Paid, ct),
                UnpaidInvoiceCount = await _context.Invoices.CountAsync(x => x.PaymentStatus == InvoiceStatuses.Unpaid, ct)
            }
        };
    }
}
