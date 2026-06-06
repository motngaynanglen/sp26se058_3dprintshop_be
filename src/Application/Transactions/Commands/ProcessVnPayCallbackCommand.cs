using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Transactions.Commands;

/// <summary>Xử lý callback VNPay (return URL trên browser hoặc IPN server-to-server).</summary>
public record ProcessVnPayCallbackCommand : IRequest<ProcessVnPayCallbackResult>
{
    public IReadOnlyDictionary<string, string> QueryParams { get; init; } = new Dictionary<string, string>();
    public bool IsIpn { get; init; }
}

public record ProcessVnPayCallbackResult(
    bool Handled,
    bool PaymentSuccess,
    string? OrderCode,
    string? RedirectUrl,
    string IpnResponse);

public class ProcessVnPayCallbackCommandHandler : IRequestHandler<ProcessVnPayCallbackCommand, ProcessVnPayCallbackResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IVnPayService _vnPayService;
    private readonly VnPaySettings _vnPaySettings;

    public ProcessVnPayCallbackCommandHandler(
        IApplicationDbContext context,
        IVnPayService vnPayService,
        IOptions<VnPaySettings> vnPaySettings)
    {
        _context = context;
        _vnPayService = vnPayService;
        _vnPaySettings = vnPaySettings.Value;
    }

    public async Task<ProcessVnPayCallbackResult> Handle(ProcessVnPayCallbackCommand request, CancellationToken cancellationToken)
    {
        var verified = _vnPayService.VerifyCallback(request.QueryParams);

        if (string.IsNullOrEmpty(verified.TxnRef))
        {
            return FailRedirect("Thiếu mã giao dịch VNPay.", request.IsIpn);
        }

        var transaction = await _context.Transactions
            .Include(t => t.Invoice)
                .ThenInclude(i => i!.Transactions)
            .Include(t => t.Invoice)
                .ThenInclude(i => i!.Order)
                    .ThenInclude(o => o.OrderItems)
            .FirstOrDefaultAsync(
                t => t.InternalCode == verified.TxnRef && t.PaymentMethod == PaymentMethods.VNPAY,
                cancellationToken);

        if (transaction == null)
        {
            return FailRedirect("Không tìm thấy giao dịch VNPay.", request.IsIpn);
        }

        if (!verified.IsValidSignature)
        {
            transaction.Note = (transaction.Note ?? "") + " [VNPay] Chữ ký không hợp lệ.";
            transaction.LastModified = CoreHelper.SystemTimeNow;
            await _context.SaveChangesAsync(cancellationToken);
            return FailRedirect("Chữ ký VNPay không hợp lệ.", request.IsIpn, transaction.Invoice.Order);
        }

        if (transaction.TransactionStatus == "SUCCESS" || transaction.TransactionStatus == "PAID")
        {
            OrderPaymentHelper.ApplySuccessfulPayment(
                transaction.Invoice,
                transaction.Invoice.Order,
                CoreHelper.SystemTimeNow);
            await _context.SaveChangesAsync(cancellationToken);
            return SuccessRedirect(transaction.Invoice.Order, request.IsIpn, alreadyPaid: true);
        }

        if (verified.IsSuccess)
        {
            transaction.TransactionStatus = "SUCCESS";
            transaction.ExternalTransactionId = verified.TransactionNo;
            transaction.PaidAt = CoreHelper.SystemTimeNow;
            transaction.Note = $"[VNPay] {verified.Message}";
            transaction.LastModified = CoreHelper.SystemTimeNow;

            OrderPaymentHelper.ApplySuccessfulPayment(transaction.Invoice, transaction.Invoice.Order, CoreHelper.SystemTimeNow);

            if (OrderPaymentHelper.IsInvoicePartiallyPaid(transaction.Invoice))
            {
                await Mainflow2DesignFlowHelper.AfterDepositPaidAsync(
                    _context,
                    transaction.Invoice.Order,
                    cancellationToken);
            }
            else
            {
                await OrderMaterialInventoryHelper.DeductMaterialAfterPaymentAsync(
                    _context,
                    transaction.Invoice.Order,
                    "system",
                    CoreHelper.SystemTimeNow,
                    cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return SuccessRedirect(transaction.Invoice.Order, request.IsIpn);
        }

        transaction.TransactionStatus = "FAILED";
        transaction.Note = $"[VNPay] Thất bại: {verified.ResponseCode}";
        transaction.LastModified = CoreHelper.SystemTimeNow;
        await _context.SaveChangesAsync(cancellationToken);

        return FailRedirect($"Thanh toán VNPay thất bại (mã {verified.ResponseCode}).", request.IsIpn, transaction.Invoice.Order);
    }

    private ProcessVnPayCallbackResult SuccessRedirect(Domain.Entities.Order order, bool isIpn, bool alreadyPaid = false)
    {
        var feBase = (_vnPaySettings.FrontendReturnUrl ?? "http://localhost:3000").TrimEnd('/');
        var redirect = $"{feBase}/order-confirmation?orderId={order.Id}&code={Uri.EscapeDataString(order.Code)}&paid=1";
        if (alreadyPaid)
            redirect = $"{feBase}/orders/{order.Id}";

        return new ProcessVnPayCallbackResult(
            Handled: true,
            PaymentSuccess: true,
            OrderCode: order.Code,
            RedirectUrl: isIpn ? null : redirect,
            IpnResponse: "OK");
    }

    private ProcessVnPayCallbackResult FailRedirect(string message, bool isIpn, Domain.Entities.Order? order = null)
    {
        var feBase = (_vnPaySettings.FrontendReturnUrl ?? "http://localhost:3000").TrimEnd('/');
        var redirect = order != null
            ? $"{feBase}/order-confirmation?orderId={order.Id}&code={Uri.EscapeDataString(order.Code)}&failed=1&message={Uri.EscapeDataString(message)}"
            : $"{feBase}/checkout?payFailed=1&message={Uri.EscapeDataString(message)}";

        return new ProcessVnPayCallbackResult(
            Handled: false,
            PaymentSuccess: false,
            OrderCode: order?.Code,
            RedirectUrl: isIpn ? null : redirect,
            IpnResponse: "FAILED");
    }
}
