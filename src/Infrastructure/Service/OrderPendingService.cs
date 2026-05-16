using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class OrderPendingService : IOrderPendingService
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<OrderPendingService> _logger;

    public OrderPendingService(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<OrderPendingService> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task EnsureCustomerHasNoPendingOrderAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var now = CoreHelper.SystemTimeNow.UtcDateTime;
        var fallbackActiveAfter = CoreHelper.SystemTimeNow.AddMinutes(-OrderPaymentConstants.PendingPaymentLifetimeMinutes);

        var hasActivePendingOrder = await _context.Orders
            .AnyAsync(o => o.CustomerId == customerId
                && o.OrderStatus == OrderStatuses.Pending
                && (
                    (o.Invoice != null && o.Invoice.DueDate.HasValue && o.Invoice.DueDate.Value > now)
                    || (o.Invoice == null && o.Created > fallbackActiveAfter)
                    || (o.Invoice != null && !o.Invoice.DueDate.HasValue && o.Created > fallbackActiveAfter)
                ), cancellationToken);

        if (hasActivePendingOrder)
        {
            throw new BusinessException(
                "Bạn đang có đơn hàng chờ thanh toán còn hiệu lực. Vui lòng hoàn tất thanh toán hoặc hủy đơn hàng cũ trước khi tạo đơn mới.",
                ResponseCodeConstants.VAL_BUSINESS_RESTRICTION);
        }
    }

    public Task<int> CancelExpiredPendingOrdersAsync(CancellationToken cancellationToken)
    {
        return CancelExpiredPendingOrdersAsync(customerId: null, cancellationToken);
    }

    private async Task<int> CancelExpiredPendingOrdersAsync(Guid? customerId, CancellationToken cancellationToken)
    {
        var now = CoreHelper.SystemTimeNow.UtcDateTime;
        var fallbackExpiredBefore = CoreHelper.SystemTimeNow.AddMinutes(-OrderPaymentConstants.PendingPaymentLifetimeMinutes);

        var query = BuildPendingOrderQuery()
            .Where(o =>
                (o.Invoice != null && o.Invoice.DueDate.HasValue && o.Invoice.DueDate.Value <= now)
                || (o.Invoice == null && o.Created <= fallbackExpiredBefore)
                || (o.Invoice != null && !o.Invoice.DueDate.HasValue && o.Created <= fallbackExpiredBefore));

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        var expiredOrders = await query.ToListAsync(cancellationToken);
        if (expiredOrders.Count == 0)
        {
            return 0;
        }

        foreach (var order in expiredOrders)
        {
            await CancelExpiredOrderAsync(order);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return expiredOrders.Count;
    }

    private IQueryable<Order> BuildPendingOrderQuery()
    {
        return _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.DesignVariant)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.DesignWork)
            .Include(o => o.Invoice)
                .ThenInclude(i => i!.Transactions)
            .Include(o => o.Shipments)
            .Where(o => o.OrderStatus == OrderStatuses.Pending);
    }

    private async Task CancelExpiredOrderAsync(Order order)
    {
        foreach (var transaction in order.Invoice?.Transactions ?? [])
        {
            if (transaction.TransactionStatus == TransactionStatuses.Success)
            {
                continue;
            }

            if (transaction.TransactionStatus == TransactionStatuses.Pending
                && transaction.PaymentMethod == PaymentMethods.PAYOS
                && long.TryParse(transaction.InternalCode, out var payOsOrderCode))
            {
                try
                {
                    await _paymentService.CancelPaymentLink(payOsOrderCode, "Đơn hàng quá hạn thanh toán.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not cancel PayOS link for expired order {OrderCode}.", order.Code);
                }
            }

            transaction.TransactionStatus = TransactionStatuses.Cancelled;
            transaction.Note = "Tự động hủy do đơn hàng quá hạn thanh toán.";
            transaction.LastModified = CoreHelper.SystemTimeNow;
            transaction.LastModifiedBy = "SYSTEM_AUTO";
        }

        foreach (var item in order.OrderItems)
        {
            item.FulfillmentStatus = OrderItemStatuses.Cancelled;
            item.LastModified = CoreHelper.SystemTimeNow;
            item.LastModifiedBy = "SYSTEM_AUTO";

            if (item.SourceType == SourceTypes.InStock && item.DesignVariant != null)
            {
                item.DesignVariant.StockQuantity += item.QuantityOrdered;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    DesignVariantId = item.DesignVariant.Id,
                    ReferenceId = order.Id,
                    Quantity = item.QuantityOrdered,
                    Type = InventoryTransactionTypes.OrderCancelReturn,
                    Note = $"Hoàn kho tự động do đơn {order.Code} quá hạn thanh toán.",
                    Created = CoreHelper.SystemTimeNow,
                    CreatedBy = "SYSTEM_AUTO",
                    LastModified = CoreHelper.SystemTimeNow,
                    LastModifiedBy = "SYSTEM_AUTO"
                });
            }

            if (item.SourceType == SourceTypes.DesignService && item.DesignWork != null)
            {
                item.DesignWork.Status = DesignWorkStatus.Sketching;
                item.DesignWork.IsLocked = false;
                item.DesignWork.LastModified = CoreHelper.SystemTimeNow;
                item.DesignWork.LastModifiedBy = "SYSTEM_AUTO";

                _context.DesignLogs.Add(new DesignLog
                {
                    Id = Guid.NewGuid(),
                    DesignWorkId = item.DesignWork.Id,
                    LogType = DesignLogType.StatusChange,
                    Content = $"Hệ thống: Đơn hàng {order.Code} quá hạn thanh toán và đã bị hủy. Phiên thiết kế được mở lại để khách hàng điều chỉnh.",
                    Created = CoreHelper.SystemTimeNow,
                    CreatedBy = "SYSTEM_AUTO",
                    LastModified = CoreHelper.SystemTimeNow,
                    LastModifiedBy = "SYSTEM_AUTO"
                });
            }
        }

        foreach (var shipment in order.Shipments)
        {
            shipment.ShipmentStatus = ShipmentStatuses.Cancelled;
            shipment.LastModified = CoreHelper.SystemTimeNow;
            shipment.LastModifiedBy = "SYSTEM_AUTO";
        }

        if (order.Invoice != null)
        {
            order.Invoice.PaymentStatus = InvoiceStatuses.Cancelled;
            order.Invoice.LastModified = CoreHelper.SystemTimeNow;
            order.Invoice.LastModifiedBy = "SYSTEM_AUTO";
        }

        order.OrderStatus = OrderStatuses.Cancelled;
        order.LastModified = CoreHelper.SystemTimeNow;
        order.LastModifiedBy = "SYSTEM_AUTO";
    }
}
