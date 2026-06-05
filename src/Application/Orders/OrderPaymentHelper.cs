using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Orders;

internal static class OrderPaymentHelper
{
    private static readonly HashSet<string> CustomProductionSourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        SourceTypes.DesignService,
        SourceTypes.PrintService,
        SourceTypes.PreOrder,
    };

    /// <summary>Sau khi thanh toán thành công — chuyển đơn sang sản xuất.</summary>
    public static void StartProductionAfterPayment(Order order, DateTimeOffset now)
    {
        order.OrderStatus = OrderStatuses.Processing;
        if (order.DepositedAt == null)
            order.DepositedAt = now;

        foreach (var item in order.OrderItems.Where(i => i.FulfillmentStatus == OrderItemStatuses.Pending))
        {
            item.FulfillmentStatus = CustomProductionSourceTypes.Contains(item.SourceType)
                ? OrderItemStatuses.Printing
                : OrderItemStatuses.Picking;
        }
    }

    public static bool IsInvoicePaid(Invoice? invoice) =>
        invoice?.PaymentStatus == InvoiceStatuses.Paid;

    public static bool IsCodOrder(Invoice? invoice)
    {
        if (invoice?.Transactions == null || invoice.Transactions.Count == 0)
            return false;

        return invoice.Transactions.Any(IsActiveCodTransaction);
    }

    public static bool IsActiveCodTransaction(Transaction transaction) =>
        transaction.PaymentMethod == PaymentMethods.Cash
        && transaction.TransactionStatus is not ("FAILED" or "CANCELLED");

    /// <summary>Đơn được phép vào sản xuất / đổi trạng thái (đã TT online hoặc COD).</summary>
    public static bool CanProceedToFulfillment(Invoice? invoice) =>
        IsInvoicePaid(invoice) || IsCodOrder(invoice);

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
