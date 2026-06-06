using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.ManagerDashboard.Models;
using sp26se058_3dprintshop_be.Application.Materials;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.ManagerDashboard.Queries;

public record GetManagerDashboardQuery : IRequest<ManagerDashboardDto>;

public class GetManagerDashboardQueryHandler : IRequestHandler<GetManagerDashboardQuery, ManagerDashboardDto>
{
    private const string TransactionSuccess = "SUCCESS";
    private const int FeedbackPreviewLimit = 10;
    private const int MonthlyTrendMonths = 6;

    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetManagerDashboardQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<ManagerDashboardDto> Handle(GetManagerDashboardQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var role = _user.Role ?? Roles.GUEST;
        if (role is not Roles.MANAGER and not Roles.ADMIN)
            throw new UnauthorizedAccessException("Chỉ quản lý hoặc admin xem được báo cáo.");

        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var trendStart = monthStart.AddMonths(-(MonthlyTrendMonths - 1));

        var successTransactions = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.TransactionStatus == TransactionSuccess)
            .Select(t => new { t.Amount, t.Created })
            .ToListAsync(cancellationToken);

        var totalCollected = successTransactions.Sum(t => t.Amount);
        var thisMonthCollected = successTransactions
            .Where(t => t.Created >= monthStart)
            .Sum(t => t.Amount);

        var pendingInvoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.PaymentStatus == InvoiceStatuses.Unpaid
                || i.PaymentStatus == InvoiceStatuses.PartiallyPaid)
            .Select(i => i.TotalAmount)
            .ToListAsync(cancellationToken);

        var materialStock = await _context.Materials
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => new MaterialStockReportItemDto
            {
                MaterialId = m.Id,
                MaterialName = m.Name,
                StockQuantityGrams = m.StockQuantityGrams,
                IsLowStock = m.StockQuantityGrams < MaterialInventoryHelper.LowStockThresholdGrams,
            })
            .OrderBy(m => m.StockQuantityGrams)
            .ThenBy(m => m.MaterialName)
            .ToListAsync(cancellationToken);

        var variantStockByMaterial = await _context.DesignVariants
            .AsNoTracking()
            .GroupBy(v => v.MaterialId)
            .Select(g => new
            {
                MaterialId = g.Key,
                TotalStock = g.Sum(v => v.StockQuantity),
                VariantCount = g.Count(),
                ActiveVariantCount = g.Count(v => v.IsActive),
                LowStockVariantCount = g.Count(v =>
                    v.MinimumStockLevel.HasValue
                    && v.StockQuantity <= v.MinimumStockLevel.Value),
            })
            .ToListAsync(cancellationToken);

        foreach (var row in materialStock)
        {
            var variantInfo = variantStockByMaterial.FirstOrDefault(v => v.MaterialId == row.MaterialId);
            if (variantInfo == null)
                continue;

            row.TotalStock = variantInfo.TotalStock;
            row.VariantCount = variantInfo.VariantCount;
            row.ActiveVariantCount = variantInfo.ActiveVariantCount;
            row.LowStockVariantCount = variantInfo.LowStockVariantCount;
        }

        var feedbackRows = await _context.Feedbacks
            .AsNoTracking()
            .Include(f => f.Customer)
                .ThenInclude(c => c.Account)
            .Include(f => f.DesignTemplate)
            .OrderByDescending(f => f.Created)
            .Take(FeedbackPreviewLimit)
            .ToListAsync(cancellationToken);

        var recentFeedbacks = feedbackRows.Select(f => new DashboardFeedbackItemDto
        {
            Id = f.Id,
            CustomerFullName = f.Customer?.Account != null
                ? (f.Customer.Account.Fullname ?? f.Customer.Account.Username)
                : "Khách hàng",
            DesignTemplateName = f.DesignTemplate?.Name,
            Rating = f.Rating,
            Comment = f.Comment,
            StaffReply = f.StaffReply,
            IsHidden = f.IsHidden,
            Created = f.Created,
        }).ToList();

        return new ManagerDashboardDto
        {
            Revenue = new RevenueReportDto
            {
                TotalCollected = totalCollected,
                ThisMonthCollected = thisMonthCollected,
                SuccessfulTransactionCount = successTransactions.Count,
                PendingInvoiceAmount = pendingInvoices.Sum(),
                PendingInvoiceCount = pendingInvoices.Count,
                MonthlyTrend = BuildMonthlyTrend(
                    successTransactions.Select(t => (t.Amount, t.Created)),
                    trendStart,
                    now),
            },
            MaterialStock = materialStock,
            RecentFeedbacks = recentFeedbacks,
        };
    }

    private static List<MonthlyRevenueItemDto> BuildMonthlyTrend(
        IEnumerable<(decimal Amount, DateTimeOffset Created)> transactions,
        DateTimeOffset trendStart,
        DateTimeOffset now)
    {
        var grouped = transactions
            .Where(t => t.Created >= trendStart)
            .GroupBy(t => new { t.Created.Year, t.Created.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.Sum(x => x.Amount));

        var result = new List<MonthlyRevenueItemDto>();
        var cursor = new DateTimeOffset(trendStart.Year, trendStart.Month, 1, 0, 0, 0, TimeSpan.Zero);

        while (cursor <= now)
        {
            grouped.TryGetValue((cursor.Year, cursor.Month), out var amount);
            result.Add(new MonthlyRevenueItemDto
            {
                Year = cursor.Year,
                Month = cursor.Month,
                Label = $"T{cursor.Month}/{cursor.Year % 100:D2}",
                Amount = amount,
            });
            cursor = cursor.AddMonths(1);
        }

        return result;
    }
}
