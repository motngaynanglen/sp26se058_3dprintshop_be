using PayOS.Models.Webhooks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Helpers;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Transactions.Commands;

public class ProcessOnlinePaymentV2Command : IRequest<string>
{
    public Webhook WebhookBody { get; set; } = null!;

    public class ProcessOnlinePaymentV2CommandHandler : IRequestHandler<ProcessOnlinePaymentV2Command, string>
    {
        private const string PayOsSuccessCode = "00";
        private const string VndCurrency = "VND";

        private readonly IPaymentService _paymentService;
        private readonly IOrderWorkflowService _orderWorkflowService;
        private readonly IApplicationDbContext _context;

        public ProcessOnlinePaymentV2CommandHandler(
            IPaymentService paymentService,
            IOrderWorkflowService orderWorkflowService,
            IApplicationDbContext context)
        {
            _paymentService = paymentService;
            _orderWorkflowService = orderWorkflowService;
            _context = context;
        }

        public async Task<string> Handle(ProcessOnlinePaymentV2Command request, CancellationToken cancellationToken)
        {
            // Fast-exit: only process genuine success events.
            if (!IsSuccessWebhook(request.WebhookBody))
                return string.Empty;

            var verifiedData = await _paymentService.VerifyWebhookData(request.WebhookBody);
            if (!IsSuccessData(verifiedData))
                return string.Empty;

            var internalCode = verifiedData.OrderCode.ToString();
            var transaction = await _context.Transactions
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Order)
                        .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(t => t.InternalCode == internalCode, cancellationToken);

            if (transaction == null)
                return string.Empty;

            var invoice = transaction.Invoice;
            var order = invoice.Order;

            // Idempotency: webhook may be delivered more than once.
            if (transaction.TransactionStatus == TransactionStatuses.Success)
                return order.Code;

            ValidatePayOsPayload(verifiedData, transaction, invoice);

            transaction.TransactionStatus = TransactionStatuses.Success;
            transaction.ExternalTransactionId = verifiedData.Reference;
            transaction.PaidAt = PaymentProcessingHelper.ParsePayOsTransactionTime(verifiedData.TransactionDateTime)
                                 ?? CoreHelper.SystemTimeNow;
            transaction.LastModified = CoreHelper.SystemTimeNow;
            transaction.LastModifiedBy = "SYSTEM_AUTO";
            transaction.Note = PaymentProcessingHelper.AppendNote(
                transaction.Note, "PayOS xác nhận thanh toán thành công qua webhook V2.");

            if (CanActivateOrder(invoice, order))
            {
                invoice.PaymentStatus = InvoiceStatuses.Paid;
                invoice.LastModified = CoreHelper.SystemTimeNow;
                invoice.LastModifiedBy = "SYSTEM_AUTO";
                await _orderWorkflowService.ActivateOrderWorkflowAsync(order, cancellationToken);
                order.LastModifiedBy = "SYSTEM_AUTO";
            }
            else
            {
                // Money received but state is unexpected — mark for manual review.
                transaction.Note = PaymentProcessingHelper.AppendNote(
                    transaction.Note,
                    $"Đã nhận tiền từ PayOS nhưng không tự kích hoạt đơn vì invoice/order đang ở trạng thái " +
                    $"{invoice.PaymentStatus}/{order.OrderStatus}. Cần xử lý thủ công.");
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new UpdateFailureException($"Lỗi cập nhật webhook PayOS V2: {ex.InnerException?.Message ?? ex.Message}");
            }

            return order.Code;
        }

        private static bool IsSuccessWebhook(Webhook webhook)
            => webhook.Success && webhook.Code == PayOsSuccessCode;

        private static bool IsSuccessData(WebhookData verifiedData)
            => verifiedData.Code == PayOsSuccessCode
            && string.Equals(verifiedData.Currency, VndCurrency, StringComparison.OrdinalIgnoreCase);

        private static void ValidatePayOsPayload(WebhookData verifiedData, Transaction transaction, Invoice invoice)
        {
            var expectedAmount = PaymentProcessingHelper.ToPayOsAmount(transaction.Amount);
            if (verifiedData.Amount != expectedAmount
                || verifiedData.Amount != PaymentProcessingHelper.ToPayOsAmount(invoice.TotalAmount))
            {
                throw new BusinessException(
                    "Số tiền PayOS gửi về không khớp với giao dịch hoặc hóa đơn.",
                    ResponseCodeConstants.VAL_BUSINESS_RESTRICTION);
            }
        }

        private static bool CanActivateOrder(Invoice invoice, Order order)
            => invoice.PaymentStatus == InvoiceStatuses.Unpaid
            && order.OrderStatus == OrderStatuses.Pending;
    }
}
