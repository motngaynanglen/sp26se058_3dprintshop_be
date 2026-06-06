using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Application.StaffDashboard.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.StaffDashboard.Queries;

/// <summary>Bàn làm việc KTV — tổng hợp việc ưu tiên từ DB (không mock).</summary>
public record GetStaffWorkbenchQuery : IRequest<StaffWorkbenchDto>;

public class GetStaffWorkbenchQueryHandler : IRequestHandler<GetStaffWorkbenchQuery, StaffWorkbenchDto>
{
    private static readonly HashSet<string> CustomSourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        SourceTypes.CustomFilePrintMainflow2,
        SourceTypes.CustomQuoteMainflow2,
        SourceTypes.AiGenerated,
        SourceTypes.PrintFromDesignMainflow2,
        SourceTypes.ReprintMainflow2,
        SourceTypes.PreOrder
    };

    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetStaffWorkbenchQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<StaffWorkbenchDto> Handle(GetStaffWorkbenchQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var role = _user.Role ?? Roles.GUEST;
        if (role != Roles.STAFF && role != Roles.MANAGER && role != Roles.ADMIN)
            throw new UnauthorizedAccessException("Chỉ nhân viên/quản lý xem được bàn làm việc.");

        var isManager = Mainflow2DesignAccess.IsManager(role);
        Guid? staffId = null;

        if (!isManager)
        {
            var staff = await Mainflow2DesignAccess.EnsureStaffAsync(
                _context, _user.Id.ToGuid(), cancellationToken);
            staffId = staff.Id;
        }

        var mf2Query = _context.DesignWorks
            .AsNoTracking()
            .Where(d => d.SourceType == SourceTypes.CustomQuoteMainflow2
                        || d.SourceType == SourceTypes.CustomFilePrintMainflow2
                        || d.SourceType == SourceTypes.AiGenerated
                        || d.SourceType == SourceTypes.PrintFromDesignMainflow2
                        || d.SourceType == SourceTypes.ReprintMainflow2);

        if (isManager)
        {
            mf2Query = mf2Query.Where(d =>
                d.Status == Mainflow2DesignWorkStatuses.Submitted
                || d.Status == Mainflow2DesignWorkStatuses.Assigned
                || d.Status == Mainflow2DesignWorkStatuses.Quoted
                || d.Status == Mainflow2DesignWorkStatuses.Negotiating);
        }
        else
        {
            mf2Query = mf2Query.Where(d =>
                d.Status == Mainflow2DesignWorkStatuses.Submitted
                || d.MainAssignedStaffId == staffId);
        }

        var mf2Designs = await mf2Query.OrderByDescending(d => d.Created).ToListAsync(cancellationToken);

        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.Invoice!).ThenInclude(i => i.Transactions)
            .Where(o => o.OrderStatus == OrderStatuses.Pending
                        || o.OrderStatus == OrderStatuses.Processing
                        || o.OrderStatus == OrderStatuses.Finished)
            .OrderByDescending(o => o.Created)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();
        var shipments = orderIds.Count == 0
            ? new Dictionary<Guid, Shipment>()
            : await _context.Shipments
                .AsNoTracking()
                .Where(s => orderIds.Contains(s.OrderId))
                .ToDictionaryAsync(s => s.OrderId, cancellationToken);

        var productionQueueOrders = await GetProductionQueueOrdersAsync(cancellationToken);

        return StaffWorkbenchBuilder.Build(new StaffWorkbenchContext
        {
            Mf2Designs = mf2Designs,
            Orders = orders,
            ShipmentsByOrderId = shipments,
            ProductionQueueCount = productionQueueOrders.Count,
            ProductionQueueOrders = productionQueueOrders,
            CurrentStaffId = staffId,
            IsManager = isManager
        });
    }

    private async Task<IReadOnlyList<(Guid Id, string Code)>> GetProductionQueueOrdersAsync(
        CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.Invoice!).ThenInclude(i => i.Transactions)
            .Where(o => o.OrderStatus == OrderStatuses.Processing)
            .Where(o => o.Invoice != null && (
                o.Invoice.PaymentStatus == InvoiceStatuses.Paid
                || o.Invoice.Transactions.Any(t =>
                    t.PaymentMethod == PaymentMethods.Cash
                    && t.TransactionStatus != "FAILED"
                    && t.TransactionStatus != "CANCELLED")))
            .Where(o => o.OrderItems.Any(oi =>
                CustomSourceTypes.Contains(oi.SourceType)
                && oi.FulfillmentStatus != OrderItemStatuses.Cancelled
                && oi.FulfillmentStatus != OrderItemStatuses.Finished))
            .OrderByDescending(o => o.Created)
            .ToListAsync(cancellationToken);

        var result = new List<(Guid Id, string Code)>();
        foreach (var order in orders)
        {
            var productionLines = order.OrderItems
                .Where(oi => CustomSourceTypes.Contains(oi.SourceType)
                             && oi.FulfillmentStatus != OrderItemStatuses.Cancelled)
                .ToList();
            if (productionLines.Count == 0)
                continue;
            if (productionLines.All(oi => oi.FulfillmentStatus == OrderItemStatuses.Finished))
                continue;
            result.Add((order.Id, order.Code));
        }

        return result;
    }
}
