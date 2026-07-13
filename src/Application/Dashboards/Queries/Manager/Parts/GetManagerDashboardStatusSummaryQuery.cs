using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Dashboards.Queries;

[Authorize(Roles = Roles.MANAGER)]
public record GetManagerDashboardStatusSummaryQuery : IRequest<ManagerDashboardStatusSummaryDTO>;

public class GetManagerDashboardStatusSummaryQueryHandler : IRequestHandler<GetManagerDashboardStatusSummaryQuery, ManagerDashboardStatusSummaryDTO>
{
    private readonly IApplicationDbContext _context;

    public GetManagerDashboardStatusSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ManagerDashboardStatusSummaryDTO> Handle(GetManagerDashboardStatusSummaryQuery request, CancellationToken ct)
    {
        var orderCounts = await DashboardStatusHelper.CountByStatusAsync(_context.Orders, x => x.OrderStatus, ct);
        var orderItemCounts = await DashboardStatusHelper.CountByStatusAsync(_context.OrderItems, x => x.FulfillmentStatus, ct);
        var shipmentCounts = await DashboardStatusHelper.CountByStatusAsync(_context.Shipments, x => x.ShipmentStatus, ct);
        var designWorkCounts = await DashboardStatusHelper.CountByStatusAsync(_context.DesignWorks, x => x.Status, ct);
        var invoiceCounts = await DashboardStatusHelper.CountByStatusAsync(_context.Invoices, x => x.PaymentStatus, ct);

        return new ManagerDashboardStatusSummaryDTO
        {
            GeneratedAt = CoreHelper.SystemTimeNow,
            OrdersByStatus = DashboardStatusHelper.Merge(OrderStatuses.All, orderCounts),
            OrderItemsByStatus = DashboardStatusHelper.Merge(OrderItemStatuses.All, orderItemCounts),
            ShipmentsByStatus = DashboardStatusHelper.Merge(ShipmentStatuses.All, shipmentCounts),
            DesignWorksByStatus = DashboardStatusHelper.Merge(DesignWorkStatus.All, designWorkCounts),
            InvoicesByStatus = DashboardStatusHelper.Merge(InvoiceStatuses.All, invoiceCounts)
        };
    }
}
