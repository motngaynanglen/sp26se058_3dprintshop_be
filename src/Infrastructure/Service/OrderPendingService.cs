using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Helpers;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;

public class OrderPendingService : IOrderPendingService
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IOrderWorkflowService _orderWorkflowService;
    private readonly ILogger<OrderPendingService> _logger;
    private readonly ExpiredPendingOrderJobOptions _options;

    public OrderPendingService(
        IApplicationDbContext context,
        IPaymentService paymentService,
        IOrderWorkflowService orderWorkflowService,
        ILogger<OrderPendingService> logger,
        IOptions<ExpiredPendingOrderJobOptions> options)
    {
        _context = context;
        _paymentService = paymentService;
        _orderWorkflowService = orderWorkflowService;
        _logger = logger;
        _options = options.Value;
    }

    // ─── Public ────────────────────────────────────────────────────────────────

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
        => ProcessExpiredOrdersAsync(customerId: null, cancellationToken);

    // ─── Core loop ─────────────────────────────────────────────────────────────

    private async Task<int> ProcessExpiredOrdersAsync(Guid? customerId, CancellationToken cancellationToken)
    {
        var now = CoreHelper.SystemTimeNow.UtcDateTime;
        var fallbackExpiredBefore = CoreHelper.SystemTimeNow.AddMinutes(-OrderPaymentConstants.PendingPaymentLifetimeMinutes);

        // Select only IDs first — avoid loading heavy object graph into RAM before knowing which orders need work.
        var query = _context.Orders
            .Where(o => o.OrderStatus == OrderStatuses.Pending
                && (
                    (o.Invoice != null && o.Invoice.DueDate.HasValue && o.Invoice.DueDate.Value <= now)
                    || (o.Invoice == null && o.Created <= fallbackExpiredBefore)
                    || (o.Invoice != null && !o.Invoice.DueDate.HasValue && o.Created <= fallbackExpiredBefore)
                ));

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        // Cap each cycle to avoid long-running job tying up connections.
        var expiredOrderIds = await query
            .OrderBy(o => o.Created)
            .Select(o => o.Id)
            .Take(Math.Max(_options.BatchSize, 1))
            .ToListAsync(cancellationToken);

        if (expiredOrderIds.Count == 0)
            return 0;

        var cancelledCount = 0;

        foreach (var orderId in expiredOrderIds)
        {
            // Check PayOS BEFORE opening a DB transaction to avoid holding locks during external HTTP calls.
            var providerCheck = await CheckPayOsStatusBeforeTransactionAsync(orderId, cancellationToken);
            if (providerCheck.ProviderCallFailed)
            {
                // Cannot determine payment state — skip this cycle to avoid cancelling a paid order.
                continue;
            }

            // Per-order isolation: if webhook commits between our ID query and now, the re-fetch below catches it.
            await using var dbTx = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var order = await LoadOrderForTransitionAsync(orderId, cancellationToken);

                // Webhook may have already moved the order out of Pending — bail out cleanly.
                if (order == null || order.OrderStatus != OrderStatuses.Pending)
                {
                    await dbTx.RollbackAsync(cancellationToken);
                    continue;
                }

                var cancelled = await ApplyDecisionAsync(order, providerCheck.PayOsStatus, cancellationToken);
                if (cancelled) cancelledCount++;

                await _context.SaveChangesAsync(cancellationToken);
                await dbTx.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await dbTx.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to process expired order {OrderId}.", orderId);
                // Rollback does not clear EF ChangeTracker — stale Modified entities must be
                // removed so the next order in this batch starts with a clean context.
                _context.ClearChangeTracker();
            }
        }

        return cancelledCount;
    }

    // ─── PayOS pre-check (outside DB transaction) ──────────────────────────────

    private async Task<ProviderCheck> CheckPayOsStatusBeforeTransactionAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var payOsCode = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.Invoice.OrderId == orderId
                && t.TransactionStatus == TransactionStatuses.Pending
                && t.PaymentMethod == PaymentMethods.PAYOS
                && t.InternalCode != null
                && t.InternalCode != string.Empty)
            .OrderByDescending(t => t.Created)
            .Select(t => t.InternalCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(payOsCode) || !long.TryParse(payOsCode, out var orderCode))
            return ProviderCheck.NoTransaction();

        try
        {
            var status = await _paymentService.GetPaymentLinkStatus(orderCode);
            return ProviderCheck.Checked(status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Cannot reach PayOS before processing expired order {OrderId}. Skipping this cycle to avoid cancelling a paid order.",
                orderId);
            return ProviderCheck.Failed();
        }
    }

    // ─── Load order (inside DB transaction) ────────────────────────────────────

    private async Task<Order?> LoadOrderForTransitionAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.DesignVariant)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.DesignWork)
            .Include(o => o.Invoice)
                .ThenInclude(i => i!.Transactions)
            .Include(o => o.Shipments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    // ─── Decision logic ────────────────────────────────────────────────────────

    /// <returns>true if order was cancelled, false otherwise.</returns>
    private async Task<bool> ApplyDecisionAsync(
        Order order,
        PayOsPaymentLinkStatusResult? payOsStatus,
        CancellationToken cancellationToken)
    {
        // No PayOS transaction found → cancel immediately (cash order, or link was never created).
        if (payOsStatus == null)
        {
            await CancelExpiredOrderAsync(order);
            return true;
        }

        if (PaymentProcessingHelper.IsPayOsStatus(payOsStatus, "Paid"))
        {
            await ActivatePaidPayOsOrderAsync(order, payOsStatus, cancellationToken);
            return false;
        }

        if (PaymentProcessingHelper.IsPayOsStatus(payOsStatus, "Pending", "Processing", "Underpaid"))
        {
            _logger.LogInformation(
                "Order {OrderCode} not cancelled — PayOS status is {Status}.",
                order.Code, payOsStatus.Status);
            return false;
        }

        if (PaymentProcessingHelper.IsPayOsStatus(payOsStatus, "Expired", "Cancelled", "Failed"))
        {
            await CancelExpiredOrderAsync(order);
            return true;
        }

        _logger.LogWarning(
            "Order {OrderCode} not cancelled — unknown PayOS status '{Status}'.",
            order.Code, payOsStatus.Status);
        return false;
    }

    // ─── Activate (PayOS already paid, job detected it before webhook arrived) ─

    private async Task ActivatePaidPayOsOrderAsync(
        Order order,
        PayOsPaymentLinkStatusResult payOsStatus,
        CancellationToken cancellationToken)
    {
        var invoice = order.Invoice;
        var transaction = GetPendingPayOsTransaction(order);

        if (invoice == null || transaction == null)
            return;

        // Guard: webhook may have committed between the ID query and this point.
        if (invoice.PaymentStatus == InvoiceStatuses.Paid || order.OrderStatus != OrderStatuses.Pending)
            return;

        var expectedAmount = PaymentProcessingHelper.ToPayOsAmount(transaction.Amount);
        if (payOsStatus.AmountPaid != expectedAmount
            || payOsStatus.AmountPaid != PaymentProcessingHelper.ToPayOsAmount(invoice.TotalAmount))
        {
            transaction.Note = PaymentProcessingHelper.AppendNote(transaction.Note,
                $"PayOS báo đã thanh toán nhưng số tiền không khớp. " +
                $"PayOS: {payOsStatus.AmountPaid}, giao dịch: {transaction.Amount}, hóa đơn: {invoice.TotalAmount}. Cần kiểm tra thủ công.");
            _logger.LogWarning(
                "Amount mismatch for order {OrderCode}. PayOS={AmountPaid}, Tx={TxAmount}, Invoice={InvoiceAmount}.",
                order.Code, payOsStatus.AmountPaid, transaction.Amount, invoice.TotalAmount);
            return;
        }

        transaction.TransactionStatus = TransactionStatuses.Success;
        transaction.ExternalTransactionId = payOsStatus.Reference;
        transaction.PaidAt = PaymentProcessingHelper.ParsePayOsTransactionTime(payOsStatus.TransactionDateTime)
                             ?? CoreHelper.SystemTimeNow;
        transaction.Note = PaymentProcessingHelper.AppendNote(transaction.Note,
            "PayOS báo đã thanh toán khi job kiểm tra đơn quá hạn. Hệ thống kích hoạt đơn thay vì hủy.");
        transaction.LastModified = CoreHelper.SystemTimeNow;
        transaction.LastModifiedBy = "SYSTEM_AUTO";

        invoice.PaymentStatus = InvoiceStatuses.Paid;
        invoice.LastModified = CoreHelper.SystemTimeNow;
        invoice.LastModifiedBy = "SYSTEM_AUTO";

        await _orderWorkflowService.ActivateOrderWorkflowAsync(order, cancellationToken);
        order.LastModifiedBy = "SYSTEM_AUTO";
    }

    // ─── Cancel expired order ──────────────────────────────────────────────────

    private async Task CancelExpiredOrderAsync(Order order)
    {
        foreach (var transaction in order.Invoice?.Transactions ?? [])
        {
            if (transaction.TransactionStatus == TransactionStatuses.Success)
                continue;

            // Attempt to cancel the PayOS link. Failure is logged but not thrown — DB cancel proceeds regardless.
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
            transaction.Note = PaymentProcessingHelper.AppendNote(
                transaction.Note, "Tự động hủy do đơn hàng quá hạn thanh toán.");
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

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static Transaction? GetPendingPayOsTransaction(Order order)
        => order.Invoice?.Transactions
            .Where(t => t.TransactionStatus == TransactionStatuses.Pending
                && t.PaymentMethod == PaymentMethods.PAYOS
                && long.TryParse(t.InternalCode, out _))
            .OrderByDescending(t => t.Created)
            .FirstOrDefault();

    // ─── Value object ──────────────────────────────────────────────────────────

    private sealed record ProviderCheck(bool ProviderCallFailed, PayOsPaymentLinkStatusResult? PayOsStatus)
    {
        /// <summary>No PayOS transaction on this order — cancel without checking provider.</summary>
        public static ProviderCheck NoTransaction() => new(false, null);

        /// <summary>Provider responded with a known status.</summary>
        public static ProviderCheck Checked(PayOsPaymentLinkStatusResult status) => new(false, status);

        /// <summary>Provider call threw — skip this cycle rather than risk cancelling a paid order.</summary>
        public static ProviderCheck Failed() => new(true, null);
    }
}
