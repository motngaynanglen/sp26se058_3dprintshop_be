using System.Security.Cryptography;
using System.Text;
using PayOS.Models.Webhooks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Transactions.Commands;

public class ProcessOnlinePaymentCommand : IRequest<string>
{
    public Webhook WebhookBody { get; set; } = null!;

    public class ProcessOnlinePaymentCommandHandler : IRequestHandler<ProcessOnlinePaymentCommand, string>
    {
        private readonly IPaymentService _paymentService;
        private readonly IOrderWorkflowService _orderWorkflowService;
        private readonly IApplicationDbContext _context;
        public ProcessOnlinePaymentCommandHandler(IPaymentService paymentService,IOrderWorkflowService orderWorkflowService, IApplicationDbContext context)
        {
            _orderWorkflowService = orderWorkflowService;
            _paymentService = paymentService;
            _context = context;
        }

        public async Task<string> Handle(ProcessOnlinePaymentCommand request, CancellationToken cancellationToken)
        {

            var verifiedData = await _paymentService.VerifyWebhookData(request.WebhookBody);
            var internalCode = verifiedData.OrderCode.ToString();


            var transaction = await _context.Transactions
                     .Include(t => t.Invoice)
                        .ThenInclude(i => i.Order)
                            .ThenInclude(o => o.OrderItems)
                     .FirstOrDefaultAsync(t => t.InternalCode == internalCode);
            if (transaction == null || transaction.TransactionStatus == TransactionStatuses.Success)
            {
                return string.Empty; // Đã xử lý hoặc không tồn tại
            }

            transaction.TransactionStatus = TransactionStatuses.Success;
            transaction.ExternalTransactionId = verifiedData.Reference;
            transaction.LastModified = CoreHelper.SystemTimeNow;

            Invoice invoice = transaction.Invoice!;
            invoice.PaymentStatus = InvoiceStatuses.Paid;
            invoice.LastModified = CoreHelper.SystemTimeNow;

            var order = invoice.Order;
            await _orderWorkflowService.ActivateOrderWorkflowAsync(order, cancellationToken);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new UpdateFailureException($"Lỗi cập nhật Webhook: {ex.Message}");
            }

            return order.Code;
        }
    }
}

