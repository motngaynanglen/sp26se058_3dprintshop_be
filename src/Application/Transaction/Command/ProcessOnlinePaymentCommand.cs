using System.Security.Cryptography;
using System.Text;
using PayOS.Models.Webhooks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace BG_IMPACT.Business.Command.Transaction.Commands
{
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
                         .ThenInclude(i => i.Order)
                         .FirstOrDefaultAsync(t => t.InternalCode == internalCode);
                if (transaction == null || transaction.TransactionStatus == "SUCCESS") {
                    return string.Empty; // Đã xử lý hoặc không tồn tại
                }

                transaction.TransactionStatus = "SUCCESS";
                transaction.ExternalTransactionId = verifiedData.Reference;

                Invoice invoice = transaction.Invoice;
                // Kiểm tra xem tổng các giao dịch thành công đã đủ tiền chưa
                // (Phòng trường hợp khách thanh toán nhiều lần cho 1 hóa đơn)
                var totalPaid = await _context.Transactions
                    .Where(t => t.InvoiceId == invoice.Id && t.TransactionStatus == "SUCCESS")
                    .SumAsync(t => t.Amount);

                //if (totalPaid >= invoice.TotalAmount)
                //{
                    invoice.PaymentStatus = InvoiceStatuses.Paid;
                    var order = invoice.Order;
                    order.OrderStatus = OrderStatuses.Processing;
                    order.DeliveredAt = DateTimeOffset.UtcNow;
                //}
                //else
                //{
                //    invoice.PaymentStatus = "Partially Paid";
                //}
                await _context.SaveChangesAsync(cancellationToken);
                return order.Code;
            }
        }
    }
}
