using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Orders;

internal static class OrderPaymentHelper
{
    private static readonly HashSet<string> CustomProductionSourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        SourceTypes.CustomFilePrintMainflow2,
        SourceTypes.CustomQuoteMainflow2,
        SourceTypes.AiGenerated,
        SourceTypes.PrintFromDesignMainflow2,
        SourceTypes.ReprintMainflow2,
        SourceTypes.PreOrder,
    };

    /// <summary>Sau khi thanh toán thành công — chuyển đơn sang sản xuất.</summary>
    public static void StartProductionAfterPayment(Order order, DateTimeOffset now)
    {
        order.OrderStatus = OrderStatuses.Processing;
        if (order.DepositedAt == null)
            order.DepositedAt = now;

        foreach (var item in order.OrderItems.Where(i =>
                     i.FulfillmentStatus is OrderItemStatuses.Pending or OrderItemStatuses.Designing))
        {
            item.FulfillmentStatus = CustomProductionSourceTypes.Contains(item.SourceType)
                ? OrderItemStatuses.Printing
                : OrderItemStatuses.Picking;
        }
    }

    /// <summary>Sau khi nhận tiền cọc — chuyển sang giai đoạn thiết kế (chưa in).</summary>
    public static void StartDesignPhaseAfterDeposit(Order order, DateTimeOffset now)
    {
        order.OrderStatus = OrderStatuses.Processing;
        order.DepositedAt ??= now;

        foreach (var item in order.OrderItems.Where(i => i.FulfillmentStatus == OrderItemStatuses.Pending))
        {
            item.FulfillmentStatus = CustomProductionSourceTypes.Contains(item.SourceType)
                                       || item.SourceType == SourceTypes.CustomQuoteMainflow2
                ? OrderItemStatuses.Designing
                : OrderItemStatuses.Picking;
        }
    }

    /// <summary>Sau khi nhận tiền cọc — bắt đầu giai đoạn thiết kế.</summary>
    public static void StartProductionAfterDeposit(Order order, DateTimeOffset now)
    {
        StartDesignPhaseAfterDeposit(order, now);
    }

    public static bool HasCustomProductionItems(Order order) =>
        !IsDirectPrintOrder(order)
        && order.OrderItems.Any(i => RequiresCustomDepositFlow(i.SourceType));

    /// <summary>Flow 2 in sẵn / in lại + Flow 3 AI — thanh toán FULL sau báo giá, không cọc/thiết kế.</summary>
    public static bool IsDirectPrintSourceType(string? sourceType) =>
        string.Equals(sourceType, SourceTypes.ReprintMainflow2, StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceType, SourceTypes.PrintFromDesignMainflow2, StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceType, SourceTypes.AiGenerated, StringComparison.OrdinalIgnoreCase);

    /// <summary>Custom cần cọc 30% + thiết kế — không áp dụng in sẵn / in lại / AI.</summary>
    public static bool RequiresCustomDepositFlow(string? sourceType) =>
        (SourceTypes.IsCustomPrintFlow(sourceType) || sourceType == SourceTypes.CustomQuoteMainflow2)
        && !IsDirectPrintSourceType(sourceType);

    /// <summary>Đơn chỉ in sẵn hoặc in lại — thanh toán FULL, không cọc.</summary>
    public static bool IsDirectPrintOrder(Order order) =>
        order.OrderItems.Count > 0
        && order.OrderItems.All(i => IsDirectPrintSourceType(i.SourceType));

    /// <summary>Alias — giữ tương thích.</summary>
    public static bool IsReprintOnlyOrder(Order order) => IsDirectPrintOrder(order);

    public static bool IsInvoicePaid(Invoice? invoice) =>
        invoice?.PaymentStatus == InvoiceStatuses.Paid;

    public static bool IsInvoicePartiallyPaid(Invoice? invoice) =>
        invoice?.PaymentStatus == InvoiceStatuses.PartiallyPaid;

    public static decimal GetPaidAmount(Invoice? invoice)
    {
        if (invoice?.Transactions == null) return 0;
        return invoice.Transactions
            .Where(t => t.TransactionStatus == "SUCCESS")
            .Sum(t => t.Amount);
    }

    public static decimal GetDepositAmount(decimal baseAmount, decimal depositPercent)
    {
        if (baseAmount <= 0) return 0;
        var pct = Math.Clamp(depositPercent, 1m, 99m);
        var deposit = Math.Round(baseAmount * pct / 100m, 0, MidpointRounding.AwayFromZero);
        return Math.Min(deposit, baseAmount);
    }

    /// <summary>Cọc custom = 30% tiền thiết kế; nếu không có tiền thiết kế thì fallback % trên tổng đơn.</summary>
    public static async Task<decimal> ResolveCustomDepositAmountAsync(
        IApplicationDbContext context,
        Order order,
        Invoice invoice,
        decimal depositPercent,
        CancellationToken ct)
    {
        var designFee = await Mainflow2QuoteFeeHelper.ResolveDesignFeeForOrderAsync(context, order, ct);
        if (designFee > 0)
            return GetDepositAmount(designFee, depositPercent);

        return GetDepositAmount(invoice.TotalAmount, depositPercent);
    }

    public static decimal GetRemainingBalance(Invoice invoice) =>
        Math.Max(0, invoice.TotalAmount - GetPaidAmount(invoice));

    public static void ApplySuccessfulPayment(Invoice invoice, Order order, DateTimeOffset now)
    {
        var totalPaid = GetPaidAmount(invoice);
        if (totalPaid >= invoice.TotalAmount)
        {
            invoice.PaymentStatus = InvoiceStatuses.Paid;
            StartProductionAfterPayment(order, now);
            return;
        }

        if (totalPaid > 0 && HasCustomProductionItems(order))
        {
            invoice.PaymentStatus = InvoiceStatuses.PartiallyPaid;
            if (order.OrderStatus == OrderStatuses.Pending)
                StartDesignPhaseAfterDeposit(order, now);
        }
    }

    public static bool IsCodOrder(Invoice? invoice)
    {
        if (invoice?.Transactions == null || invoice.Transactions.Count == 0)
            return false;

        return invoice.Transactions.Any(IsActiveCodTransaction);
    }

    public static bool IsActiveCodTransaction(Transaction transaction) =>
        transaction.PaymentMethod == PaymentMethods.Cash
        && transaction.TransactionStatus is not ("FAILED" or "CANCELLED");

    /// <summary>Đơn được phép vào sản xuất / đổi trạng thái in (đã TT đủ hoặc COD).</summary>
    public static bool CanProceedToFulfillment(Invoice? invoice) =>
        IsInvoicePaid(invoice) || IsCodOrder(invoice);

    /// <summary>Đã đặt cọc — NV có thể làm thiết kế trong chat.</summary>
    public static bool IsInDesignPhaseAfterDeposit(Invoice? invoice) =>
        IsInvoicePartiallyPaid(invoice) && !IsInvoicePaid(invoice);

    public static string? ResolvePaymentMethod(Invoice? invoice)
    {
        if (invoice?.Transactions == null || invoice.Transactions.Count == 0)
            return null;

        var active = invoice.Transactions
            .Where(t => t.TransactionStatus is not ("FAILED" or "CANCELLED"))
            .OrderByDescending(t => t.Created)
            .FirstOrDefault();

        return active?.PaymentMethod;
    }
}
