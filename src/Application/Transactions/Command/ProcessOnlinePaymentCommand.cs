using PayOS.Models.Webhooks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Transactions.Commands;

public class ProcessOnlinePaymentCommand : IRequest<string>
{
    public Webhook WebhookBody { get; set; } = null!;

    public class ProcessOnlinePaymentCommandHandler : IRequestHandler<ProcessOnlinePaymentCommand, string>
    {
        private readonly IPaymentService _paymentService;
        private readonly IApplicationDbContext _context;
        public ProcessOnlinePaymentCommandHandler(IPaymentService paymentService, IApplicationDbContext context)
        {
            _paymentService = paymentService;
            _context = context;
        }

        public async Task<string> Handle(ProcessOnlinePaymentCommand request, CancellationToken cancellationToken)
        {

            var verifiedData = await _paymentService.VerifyWebhookData(request.WebhookBody);
            var internalCode = verifiedData.OrderCode.ToString();


            var transaction = await _context.Transactions
                     .Include(t => t.Invoice)
                     .ThenInclude(i => i!.Transactions)
                     .Include(t => t.Invoice)
                     .ThenInclude(i => i!.Order)
                         .ThenInclude(o => o.OrderItems)
                     .FirstOrDefaultAsync(t => t.InternalCode == internalCode, cancellationToken);
            if (transaction == null || transaction.TransactionStatus == "SUCCESS")
            {
                return string.Empty; // Đã xử lý hoặc không tồn tại
            }

            transaction.TransactionStatus = "SUCCESS";
            transaction.ExternalTransactionId = verifiedData.Reference;
            transaction.PaidAt = CoreHelper.SystemTimeNow;

            Invoice invoice = transaction.Invoice;
            OrderPaymentHelper.ApplySuccessfulPayment(invoice, invoice.Order, CoreHelper.SystemTimeNow);

            if (OrderPaymentHelper.IsInvoicePartiallyPaid(invoice))
            {
                await Mainflow2DesignFlowHelper.AfterDepositPaidAsync(
                    _context,
                    invoice.Order,
                    cancellationToken);
            }
            else
            {
                await OrderMaterialInventoryHelper.DeductMaterialAfterPaymentAsync(
                    _context,
                    invoice.Order,
                    "system",
                    CoreHelper.SystemTimeNow,
                    cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return invoice.Order.Code;
        }
    }
}

