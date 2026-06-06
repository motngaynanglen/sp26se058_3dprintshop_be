using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Orders.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.Orders.Queries;

/// <summary>[Staff/Manager] Hàng đợi sản xuất — đơn đã thanh toán, đang PROCESSING, dòng custom/AI chưa hoàn thiện in.</summary>
public class GetProductionQueueQuery : PaginationRequest, IRequest<PaginatedList<ProductionQueueOrderDto>>
{
    public string? Search { get; init; }

    /// <summary>PRINTING | PENDING | FINISHED | ALL</summary>
    public string? FulfillmentFilter { get; init; }
}

public class GetProductionQueueQueryHandler
    : IRequestHandler<GetProductionQueueQuery, PaginatedList<ProductionQueueOrderDto>>
{
    private static readonly HashSet<string> CustomSourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        SourceTypes.CustomFilePrintMainflow2,
        SourceTypes.CustomQuoteMainflow2,
        SourceTypes.AiGenerated,
        SourceTypes.PreOrder,
        SourceTypes.PrintFromDesignMainflow2,
        SourceTypes.ReprintMainflow2,
    };

    private static readonly HashSet<string> ActiveFulfillment = new(StringComparer.OrdinalIgnoreCase)
    {
        OrderItemStatuses.Pending,
        OrderItemStatuses.Designing,
        OrderItemStatuses.Printing
    };

    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetProductionQueueQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<PaginatedList<ProductionQueueOrderDto>> Handle(
        GetProductionQueueQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var role = _user.Role ?? Roles.GUEST;
        if (role != Roles.STAFF && role != Roles.MANAGER && role != Roles.ADMIN)
            throw new UnauthorizedAccessException("Chỉ nhân viên/quản lý xem được hàng đợi sản xuất.");

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.Customer).ThenInclude(c => c.Account)
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
                && oi.FulfillmentStatus != OrderItemStatuses.Finished));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(o =>
                o.Code.Contains(term)
                || o.Customer.Account.Username.Contains(term)
                || o.Customer.Account.Fullname.Contains(term));
        }

        var orders = await query
            .OrderByDescending(o => o.Created)
            .ToListAsync(cancellationToken);

        var shipmentByOrder = await _context.Shipments
            .AsNoTracking()
            .Where(s => orders.Select(o => o.Id).Contains(s.OrderId))
            .ToDictionaryAsync(s => s.OrderId, cancellationToken);

        var dtos = new List<ProductionQueueOrderDto>();
        foreach (var order in orders)
        {
            var lines = order.OrderItems
                .Where(oi => CustomSourceTypes.Contains(oi.SourceType)
                             && oi.FulfillmentStatus != OrderItemStatuses.Cancelled)
                .Select(oi => new ProductionQueueLineDto
                {
                    OrderItemId = oi.Id,
                    ItemName = oi.ItemName ?? "Sản phẩm",
                    SourceType = oi.SourceType,
                    FulfillmentStatus = oi.FulfillmentStatus,
                    QuantityOrdered = oi.QuantityOrdered,
                    DesignWorkId = oi.DesignWorkId
                })
                .ToList();

            if (lines.Count == 0)
                continue;

            var filter = request.FulfillmentFilter?.Trim().ToUpperInvariant();
            if (filter is "PRINTING")
                lines = lines.Where(l => l.FulfillmentStatus == OrderItemStatuses.Printing).ToList();
            else if (filter is "PENDING")
                lines = lines.Where(l => l.FulfillmentStatus == OrderItemStatuses.Pending).ToList();
            else if (filter is "FINISHED")
                lines = lines.Where(l => l.FulfillmentStatus == OrderItemStatuses.Finished).ToList();

            if (lines.Count == 0 && filter is not null and not "ALL")
                continue;

            var productionLines = order.OrderItems
                .Where(oi => CustomSourceTypes.Contains(oi.SourceType)
                             && oi.FulfillmentStatus != OrderItemStatuses.Cancelled)
                .ToList();

            var allFinished = productionLines.All(oi => oi.FulfillmentStatus == OrderItemStatuses.Finished);
            var pendingPrint = productionLines.Count(oi => ActiveFulfillment.Contains(oi.FulfillmentStatus));

            if (allFinished)
                continue;

            shipmentByOrder.TryGetValue(order.Id, out var ship);

            dtos.Add(new ProductionQueueOrderDto
            {
                OrderId = order.Id,
                OrderCode = order.Code,
                CustomerName = order.Customer.Account.Fullname ?? order.Customer.Account.Username,
                OrderStatus = order.OrderStatus,
                ShipmentStatus = ship?.ShipmentStatus,
                PaymentStatus = order.Invoice!.PaymentStatus,
                Created = order.Created,
                TotalPrice = order.TotalPrice,
                Lines = lines,
                AllLinesFinished = false,
                PendingPrintCount = pendingPrint
            });
        }

        var count = dtos.Count;
        var page = dtos
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PaginatedList<ProductionQueueOrderDto>(page, count, request.PageNumber, request.PageSize);
    }
}
