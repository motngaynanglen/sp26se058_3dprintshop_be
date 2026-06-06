using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Transactions.Command;

public record PerformTransactionCommand : IRequest<object>
{
    public required Guid OrderId { get; init; }

    [DefaultValue(PaymentMethods.PAYOS)]
    public required string PaymentMethod { get; init; }

    public string? ClientIp { get; init; }

    /// <summary>FULL (mặc định — catalog) | DEPOSIT | BALANCE — DEPOSIT/BALANCE chỉ cho đơn custom MF2.</summary>
    [DefaultValue(PaymentPhases.Full)]
    public string PaymentPhase { get; init; } = PaymentPhases.Full;
}

public class PerformTransactionCommandValidator : AbstractValidator<PerformTransactionCommand>
{
    public PerformTransactionCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.PaymentMethod).NotEmpty();
    }
}

public class PerformTransactionCommandHandler : IRequestHandler<PerformTransactionCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IVnPayService _vnPayService;
    private readonly PayOsSettings _payOsSettings;
    private readonly PaymentOptions _paymentOptions;
    private readonly IUser _user;
    private readonly ILogger<PerformTransactionCommandHandler> _logger;

    public PerformTransactionCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        IVnPayService vnPayService,
        IOptions<PayOsSettings> payOsSettings,
        IOptions<PaymentOptions> paymentOptions,
        IUser user,
        ILogger<PerformTransactionCommandHandler> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _vnPayService = vnPayService;
        _payOsSettings = payOsSettings.Value;
        _paymentOptions = paymentOptions.Value;
        _user = user;
        _logger = logger;
    }

    public async Task<object> Handle(PerformTransactionCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Invoice)
                .ThenInclude(i => i!.Transactions)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        var phase = NormalizePhase(request.PaymentPhase);
        if (OrderPaymentHelper.IsDirectPrintOrder(order))
            phase = PaymentPhases.Full;

        var chargeAmount = await ResolveChargeAmountAsync(order, phase, cancellationToken);

        ValidateOrderForTransaction(order, phase, chargeAmount);

        if (phase == PaymentPhases.Balance
            && !await Mainflow2DesignFlowHelper.CanPayBalanceAsync(_context, order, cancellationToken))
        {
            throw new InvalidOperationException(
                "Kỹ thuật viên chưa gửi bảng thiết kế — chưa thể thanh toán phần còn lại.");
        }

        if (request.PaymentMethod == PaymentMethods.PAYOS)
        {
            var existingPayment = TryGetValidPendingPayment(order.Invoice!, request.PaymentMethod, chargeAmount);
            if (existingPayment != null)
            {
                LogPaymentRedirect(request.OrderId, request.PaymentMethod, existingPayment.PaymentLink, reused: true);
                return existingPayment;
            }
        }
        else if (request.PaymentMethod == PaymentMethods.VNPAY)
        {
            InvalidatePendingVnPayTransactions(order.Invoice!);
        }

        var result = await CreatePaymentAsync(order, request.PaymentMethod, request.ClientIp, chargeAmount, phase, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        LogPaymentRedirect(request.OrderId, request.PaymentMethod, ExtractPaymentLink(result), reused: false);
        return result;
    }

    private static string NormalizePhase(string? raw) =>
        (raw ?? PaymentPhases.Full).Trim().ToUpperInvariant() switch
        {
            PaymentPhases.Deposit => PaymentPhases.Deposit,
            PaymentPhases.Balance => PaymentPhases.Balance,
            _ => PaymentPhases.Full
        };

    private async Task<decimal> ResolveChargeAmountAsync(Order order, string phase, CancellationToken ct)
    {
        var invoice = order.Invoice!;
        var isCustom = OrderPaymentHelper.HasCustomProductionItems(order);

        return phase switch
        {
            PaymentPhases.Deposit when isCustom =>
                await OrderPaymentHelper.ResolveCustomDepositAmountAsync(
                    _context, order, invoice, _paymentOptions.CustomOrderDepositPercent, ct),
            PaymentPhases.Balance when isCustom =>
                OrderPaymentHelper.GetRemainingBalance(invoice),
            _ => invoice.TotalAmount
        };
    }

    private void ValidateOrderForTransaction(Order order, string phase, decimal chargeAmount)
    {
        var invoice = order.Invoice ?? throw new InvalidOperationException("Đơn hàng chưa có hóa đơn.");
        var isCustom = OrderPaymentHelper.HasCustomProductionItems(order);
        var paid = OrderPaymentHelper.GetPaidAmount(invoice);

        if (OrderPaymentHelper.IsInvoicePaid(invoice))
            throw new InvalidOperationException("Đơn hàng đã được thanh toán. Không thể tạo giao dịch mới.");

        if (chargeAmount <= 0)
            throw new InvalidOperationException("Số tiền thanh toán phải lớn hơn 0.");

        if (phase is PaymentPhases.Deposit or PaymentPhases.Balance)
        {
            if (OrderPaymentHelper.IsDirectPrintOrder(order))
                throw new InvalidOperationException("Đơn in sẵn/in lại chỉ thanh toán một lần (paymentPhase=FULL).");

            if (!isCustom)
                throw new InvalidOperationException("Thanh toán cọc/phần còn lại chỉ áp dụng cho đơn in custom.");

            if (phase == PaymentPhases.Deposit && paid > 0)
                throw new InvalidOperationException("Đơn hàng đã có khoản thanh toán — dùng BALANCE để trả phần còn lại.");

            if (phase == PaymentPhases.Balance && !OrderPaymentHelper.IsInvoicePartiallyPaid(invoice))
                throw new InvalidOperationException("Cần đặt cọc trước khi thanh toán phần còn lại.");
        }
        else if (isCustom && !OrderPaymentHelper.IsDirectPrintOrder(order))
        {
            throw new InvalidOperationException(
                "Đơn in custom yêu cầu đặt cọc trước (paymentPhase=DEPOSIT). Phần còn lại thanh toán khi nhận hàng.");
        }

        if (phase == PaymentPhases.Full && order.OrderStatus != OrderStatuses.Pending)
            throw new InvalidOperationException($"Đơn hàng đang ở trạng thái {order.OrderStatus}, không thể thanh toán thêm.");

        if (phase == PaymentPhases.Balance
            && order.OrderStatus is not (OrderStatuses.Pending or OrderStatuses.Processing))
            throw new InvalidOperationException($"Đơn hàng đang ở trạng thái {order.OrderStatus}, không thể thanh toán phần còn lại.");
    }

    private static void InvalidatePendingVnPayTransactions(Invoice invoice)
    {
        foreach (var tx in invoice.Transactions.Where(t =>
                     t.TransactionStatus == "PENDING" && t.PaymentMethod == PaymentMethods.VNPAY))
        {
            tx.TransactionStatus = "FAILED";
            tx.Note = (tx.Note ?? "") + " [VNPay] Thay bằng link thanh toán mới.";
        }
    }

    private PaymentResponse? TryGetValidPendingPayment(Invoice invoice, string paymentMethod, decimal expectedAmount)
    {
        var pendingTransaction = invoice.Transactions.FirstOrDefault(t =>
            t.TransactionStatus == "PENDING"
            && t.PaymentMethod == paymentMethod
            && t.Amount == expectedAmount);

        if (pendingTransaction == null) return null;

        if (pendingTransaction.Created.AddMinutes(15) <= CoreHelper.SystemTimeNow)
        {
            pendingTransaction.TransactionStatus = "FAILED";
            pendingTransaction.Note = "Link cũ đã hết hạn";
            return null;
        }

        if (!string.IsNullOrEmpty(pendingTransaction.InternalCode)
            && !string.IsNullOrEmpty(pendingTransaction.PaymentLink))
        {
            return new PaymentResponse
            {
                PaymentCode = pendingTransaction.InternalCode!.ToLong(),
                PaymentLink = pendingTransaction.PaymentLink!,
                QrCode = pendingTransaction.QrCode ?? string.Empty,
            };
        }

        return null;
    }

    private async Task<object> CreatePaymentAsync(
        Order order,
        string paymentMethod,
        string? clientIp,
        decimal chargeAmount,
        string phase,
        CancellationToken cancellationToken)
    {
        var method = paymentMethod.Trim().ToUpperInvariant();
        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? "customer";
        var phaseNote = phase == PaymentPhases.Full ? "toàn bộ" : phase == PaymentPhases.Deposit ? "đặt cọc" : "phần còn lại";

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            InvoiceId = order.Invoice!.Id,
            Invoice = order.Invoice,
            Amount = chargeAmount,
            PaymentMethod = method,
            InternalCode = "PENDING",
            TransactionStatus = "PENDING",
            Note = $"[{phase}] Thanh toán {phaseNote} cho đơn {order.Code}",
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        };

        if (method == PaymentMethods.PAYOS)
        {
            if (!_payOsSettings.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Cổng PayOS chưa được cấu hình (thiếu ClientId / ApiKey / ChecksumKey trong appsettings). " +
                    "Vui lòng chọn thanh toán COD để test local, hoặc điền PayOS vào appsettings.Development.json.");
            }

            var returnUrl = string.IsNullOrWhiteSpace(_payOsSettings.ReturnUrl)
                ? "http://localhost:3000/order-confirmation"
                : _payOsSettings.ReturnUrl;
            var cancelUrl = string.IsNullOrWhiteSpace(_payOsSettings.CancelUrl)
                ? "http://localhost:3000/checkout"
                : _payOsSettings.CancelUrl;

            var paymentResponse = await _paymentService.CreatePaymentLink(order, returnUrl, cancelUrl, chargeAmount);
            transaction.InternalCode = paymentResponse.PaymentCode.ToString();
            transaction.TransactionStatus = "PENDING";
            transaction.PaymentLink = paymentResponse.PaymentLink;
            transaction.QrCode = paymentResponse.QrCode;
            transaction.Note = $"[{phase}] Tạo link PayOS — {chargeAmount:N0} VND cho đơn {order.Code}";

            _context.Transactions.Add(transaction);
            return paymentResponse;
        }

        if (method == PaymentMethods.VNPAY)
        {
            var ip = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp;
            var paymentResponse = _vnPayService.CreatePaymentUrl(order, ip, chargeAmount)
                ?? throw new InvalidOperationException("Lỗi kết nối cổng thanh toán VNPay Sandbox");

            transaction.InternalCode = paymentResponse.PaymentCode.ToString();
            transaction.TransactionStatus = "PENDING";
            transaction.PaymentLink = paymentResponse.PaymentLink;
            transaction.QrCode = string.Empty;
            transaction.Note = $"[{phase}] Tạo link VNPay — {chargeAmount:N0} VND cho đơn {order.Code}";

            _context.Transactions.Add(transaction);
            return paymentResponse;
        }

        if (method == PaymentMethods.Cash)
        {
            if (phase == PaymentPhases.Deposit)
                throw new InvalidOperationException("Tiền cọc đơn custom cần thanh toán online (VNPay/PayOS).");

            if (phase == PaymentPhases.Full)
            {
                transaction.InternalCode = $"CASH-{order.Code}-{DateTime.UtcNow.Ticks}";
                transaction.TransactionStatus = "PENDING";
                transaction.Note = $"Thanh toán COD khi nhận hàng — đơn {order.Code}";
                _context.Transactions.Add(transaction);
                OrderPaymentHelper.StartProductionAfterPayment(order, now);
                await OrderMaterialInventoryHelper.DeductMaterialAfterPaymentAsync(
                    _context,
                    order,
                    _user.Username ?? "system",
                    now,
                    cancellationToken);
                return new { Message = "Đặt hàng COD thành công — thanh toán khi nhận hàng", OrderCode = order.Code };
            }

            transaction.InternalCode = $"CASH-{order.Code}-{DateTime.UtcNow.Ticks}";
            transaction.TransactionStatus = "SUCCESS";
            transaction.PaidAt = now;
            transaction.Note = $"COD — thu phần còn lại khi giao hàng, đơn {order.Code}";
            _context.Transactions.Add(transaction);
            OrderPaymentHelper.ApplySuccessfulPayment(order.Invoice, order, now);
            await OrderMaterialInventoryHelper.DeductMaterialAfterPaymentAsync(
                _context,
                order,
                _user.Username ?? "system",
                now,
                cancellationToken);

            return new { Message = "Ghi nhận thanh toán phần còn lại (COD) thành công", OrderCode = order.Code };
        }

        throw new InvalidOperationException($"Phương thức thanh toán không hỗ trợ: {paymentMethod}");
    }

    private void LogPaymentRedirect(Guid orderId, string paymentMethod, string? paymentUrl, bool reused)
    {
        if (string.IsNullOrWhiteSpace(paymentUrl))
            return;

        _logger.LogInformation(
            "[Payment] Trước khi chuyển cổng thanh toán — OrderId={OrderId} Method={Method} ReusedLink={Reused} PaymentUrl={PaymentUrl}",
            orderId,
            paymentMethod,
            reused,
            paymentUrl);
    }

    private static string? ExtractPaymentLink(object result)
    {
        if (result is PaymentResponse pr)
            return pr.PaymentLink;

        var type = result.GetType();
        return type.GetProperty("PaymentLink")?.GetValue(result)?.ToString()
               ?? type.GetProperty("paymentLink")?.GetValue(result)?.ToString();
    }
}
