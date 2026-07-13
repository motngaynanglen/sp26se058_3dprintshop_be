using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardInventoryQuery : IRequest<ManagerDashboardInventorySummaryDTO>;

public class GetManagerDashboardInventoryQueryHandler : IRequestHandler<GetManagerDashboardInventoryQuery, ManagerDashboardInventorySummaryDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardInventoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ManagerDashboardInventorySummaryDTO> Handle(GetManagerDashboardInventoryQuery request, CancellationToken ct)
    {
        return new ManagerDashboardInventorySummaryDTO
        {
            GeneratedAt = CoreHelper.SystemTimeNow,
            LowStockVariants = await _context.DesignVariants
                .Where(x => x.IsActive && x.StockQuantity <= (x.MinimumStockLevel ?? 5))
                .OrderBy(x => x.StockQuantity)
                .Take(10)
                .Select(x => new DashboardLowStockVariantDTO
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    StockQuantity = x.StockQuantity,
                    MinimumStockLevel = x.MinimumStockLevel ?? 5
                })
                .ToListAsync(ct)
        };
    }
}
