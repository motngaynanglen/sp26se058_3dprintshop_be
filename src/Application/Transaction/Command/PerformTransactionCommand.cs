using sp26se058_3dprintshop_be.Application.Common.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using System.ComponentModel.DataAnnotations;
using PayOS;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using System.ComponentModel;

namespace sp26se058_3dprintshop_be.Application.Transaction.Commands
{
    public class PerformTransactionCommand : IRequest<object>
    {
        [Required]
        public Guid OrderId { get; set; }
        //[Required]
        //[DefaultValue(1)]
        //public int Type { get; set; }

        public class PerformTransactionCommandHandler : IRequestHandler<PerformTransactionCommand, object>
        {
            private readonly IApplicationDbContext _context;
            private readonly IPaymentService _paymentService;
            private readonly PayOsSettings _payOsSettings;

            public PerformTransactionCommandHandler(IApplicationDbContext context, IPaymentService paymentService, IOptions<PayOsSettings> payOsSettings)
            {
                _context = context;
                _paymentService = paymentService;
                _payOsSettings = payOsSettings.Value;
            }

            public async Task<object> Handle(PerformTransactionCommand request, CancellationToken cancellationToken)
            {
                // 1. Lấy thông tin Order kèm theo Invoice và các Transaction liên quan
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Invoice)
                        .ThenInclude(i => i!.Transactions)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

                if (order == null)
                    throw new Exception("Không tìm thấy đơn hàng");

                // --- TRƯỜNG HỢP 1: Đơn hàng đã được xử lý thanh toán trước đó ---
                if (order.OrderStatus != "PENDING" || (order.Invoice != null && order.Invoice.PaymentStatus == "PAID"))
                {
                    throw new Exception("Đơn hàng đã được thanh toán hoặc không ở trạng thái chờ");
                }

                // --- TRƯỜNG HỢP 3 (Bổ sung): Đảm bảo luôn có Invoice trước khi tạo Transaction ---
                if (order.Invoice == null)
                {
                    order.Invoice = new Domain.Entities.Invoice
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        InvoiceCode = $"INV-{DateTime.Now:yyyyMMdd}-{order.Code}",
                        TotalAmount = order.TotalPrice,
                        PaymentStatus = "Unpaid",
                        Created = DateTimeOffset.UtcNow
                    };
                    // Lưu tạm hoặc để EF tự track khi save cuối cùng
                }

                // --- TRƯỜNG HỢP 2: Đã có giao dịch nhưng chưa thanh toán xong ---
                var pendingTransaction = order.Invoice.Transactions
                    .FirstOrDefault(t => t.TransactionStatus == "PENDING" && !string.IsNullOrEmpty(t.PaymentLink));

                if (pendingTransaction != null)
                {
                    // Kiểm tra thời hạn 10 phút của link PayOS
                    if (pendingTransaction.Created.AddMinutes(10) > DateTimeOffset.UtcNow
                        && pendingTransaction.InternalCode != null
                        && pendingTransaction.PaymentLink != null
                        && pendingTransaction.QrCode != null
                        )
                    {
                        // Message = "Sử dụng lại link thanh toán còn hiệu lực",
                        return new PaymentResponse
                        {
                            PaymentCode = pendingTransaction.InternalCode.ToLong(),
                            PaymentLink = pendingTransaction.PaymentLink,
                            QrCode = pendingTransaction.QrCode
                        };
                    }

                    // Nếu hết hạn, đánh dấu thất bại để tạo vòng đời mới
                    pendingTransaction.TransactionStatus = "FAILED";
                    pendingTransaction.Note = "Link cũ đã hết hạn";
                }

                // --- TIẾN HÀNH TẠO TRANSACTION MỚI (Dùng cho cả TH 2 hết hạn và TH 3 mới hoàn toàn) ---
                string returnUrl = _payOsSettings.ReturnUrl;
                string cancelUrl = _payOsSettings.CancelUrl;

                var paymentResponse = await _paymentService.CreatePaymentLink(order, returnUrl, cancelUrl);

                if (paymentResponse == null)
                    throw new Exception("Lỗi kết nối cổng thanh toán");

                var newTransaction = new Domain.Entities.Transaction
                {
                    Id = Guid.NewGuid(),
                    Invoice = order.Invoice,
                    InvoiceId = order.Invoice.Id,
                    InternalCode = paymentResponse.PaymentCode.ToString(),
                    Amount = order.TotalPrice,
                    PaymentMethod = "PayOS",
                    TransactionStatus = "PENDING",
                    PaymentLink = paymentResponse.PaymentLink,
                    QrCode = paymentResponse.QrCode,
                    Created = DateTimeOffset.UtcNow,
                    Note = $"Tạo link thanh toán mới cho đơn hàng {order.Code}"
                };

                _context.Transactions.Add(newTransaction);

                // Lưu tất cả thay đổi (Invoice mới nếu có, Transaction cũ cập nhật, Transaction mới thêm)
                await _context.SaveChangesAsync(cancellationToken);

                return paymentResponse;
            }
        }
    }
}
