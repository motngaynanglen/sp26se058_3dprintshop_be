using sp26se058_3dprintshop_be.Application.Common.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using System.ComponentModel.DataAnnotations;
using PayOS;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using System.ComponentModel;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Transactions.Commands;

public record PerformTransactionCommand : IRequest<object>
{
    [Required]
    public Guid OrderId { get; init; }
    [Required]
    [DefaultValue(PaymentMethods.PAYOS)]
    public string PaymentMethod { get; init; } = null!;
}
public class PerformTransactionCommandHandler : IRequestHandler<PerformTransactionCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly PayOsSettings _payOsSettings;
    private readonly IUser _user;

    public PerformTransactionCommandHandler(IApplicationDbContext context, IPaymentService paymentService, IOptions<PayOsSettings> payOsSettings, IUser user)
    {
        _context = context;
        _paymentService = paymentService;
        _payOsSettings = payOsSettings.Value;
        _user = user;
    }

    public async Task<object> Handle(PerformTransactionCommand request, CancellationToken cancellationToken)
    {
        //if (request.PaymentMethod == PaymentMethods.Cash && _user.Role == Roles.CUSTOMER)
        //{
        //    throw new ForbiddenAccessException("Khách hàng không được tự xác nhận thanh toán tiền mặt.");
        //}

        // 1. Lấy thông tin Order kèm theo Invoice và các Transaction liên quan
        var order = await GetOrderWithDetailsAsync(request.OrderId, cancellationToken);

        // 2. Kiểm tra điều kiện đơn hàng
        ValidateOrderForTransaction(order);

        // 3. Đảm bảo luôn có Invoice (Tạo object nếu chưa có)
        EnsureOrderHasInvoice(order);

        // 4. Kiểm tra và sử dụng lại giao dịch PENDING còn hiệu lực (nếu có)
        if (request.PaymentMethod == PaymentMethods.PAYOS)
        {
            var existingPayment = TryGetValidPendingPayment(order.Invoice!); // Bước 3 bảo đảm Invoice không null
            if (existingPayment != null) return existingPayment;
        }
        if (request.PaymentMethod == PaymentMethods.Cash)
        {
             ProcessOrderWorkflowAfterPayment(order);
        }
        // 5. Tạo Transaction mới dựa trên phương thức thanh toán
        var result = await CreateTransactionByMethodAsync(order, request.PaymentMethod, cancellationToken);

        // 6. Lưu tất cả thay đổi (Invoice mới nếu có, Transaction cũ cập nhật, Transaction mới thêm)
        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }

    #region Private Helper Methods
    private async Task<Order> GetOrderWithDetailsAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Invoice)
                .ThenInclude(i => i!.Transactions)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null) throw new Exception("Không tìm thấy đơn hàng");
        return order;
    }

    private bool ValidateOrderForTransaction(Order order)
    {
        bool isNotPending = order.OrderStatus != OrderStatuses.Pending;
        bool isPaid = order.Invoice != null && order.Invoice.PaymentStatus == InvoiceStatuses.Paid;

        if (isNotPending || isPaid)
        {
            throw new Exception("Đơn hàng đã được thanh toán hoặc không ở trạng thái chờ");
        }
        return true;
    }

    private bool EnsureOrderHasInvoice(Order order)
    {
        if (order.Invoice == null)
        {
            order.Invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                InvoiceCode = $"INV-{DateTime.Now:yyyyMMdd}-{order.Code}",
                TotalAmount = order.TotalPrice,
                PaymentStatus = InvoiceStatuses.Unpaid,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username,
                LastModified = CoreHelper.SystemTimeNow,
                LastModifiedBy = _user.Username,
            };
        }
        return true;
    }

    private PaymentResponse? TryGetValidPendingPayment(Invoice invoice)
    {
        var pendingTransaction = invoice.Transactions.FirstOrDefault(t => t.TransactionStatus == "PENDING" && t.PaymentMethod == PaymentMethods.PAYOS);

        if (pendingTransaction == null) return null;

        // Kiểm tra thời hạn 10 phút
        bool isTimeOut = pendingTransaction.Created.AddMinutes(10) > CoreHelper.SystemTimeNow;
        if (isTimeOut)
        {
            pendingTransaction.TransactionStatus = "FAILED";
            pendingTransaction.Note = "Link cũ đã hết hạn";
            return null;
        }


        bool isValid = !string.IsNullOrEmpty(pendingTransaction.InternalCode)
                    && !string.IsNullOrEmpty(pendingTransaction.PaymentLink)
                    && !string.IsNullOrEmpty(pendingTransaction.QrCode);
        if (isValid)
        {
            return new PaymentResponse
            {
                PaymentCode = pendingTransaction.InternalCode!.ToLong(),
                PaymentLink = pendingTransaction.PaymentLink!,
                QrCode = pendingTransaction.QrCode!
            };
        }

        // Trước mắt các trường hợp khác không hỗ trợ
        // Cash không tồn tại khả năng PENDING.
        return null;
    }

    private async Task<object> CreateTransactionByMethodAsync(Order order, string method, CancellationToken ct)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Invoice = order.Invoice!,
            InvoiceId = order.Invoice!.Id,
            Amount = order.TotalPrice,
            PaymentMethod = method,
            InternalCode = string.Empty,
            TransactionStatus = "PENDING",
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username
        };

        if (method == PaymentMethods.PAYOS)
        {
            // Xử lý tạo Link thanh toán online
            var paymentResponse = await _paymentService.CreatePaymentLink(order, _payOsSettings.ReturnUrl, _payOsSettings.CancelUrl);
            if (paymentResponse == null) throw new Exception("Lỗi kết nối cổng thanh toán PayOS");

            transaction.InternalCode = paymentResponse.PaymentCode.ToString();
            transaction.TransactionStatus = "PENDING";
            transaction.PaymentLink = paymentResponse.PaymentLink;
            transaction.QrCode = paymentResponse.QrCode;
            transaction.Note = $"Tạo link thanh toán PayOS cho đơn hàng {order.Code}";

            _context.Transactions.Add(transaction);
            return paymentResponse;
        }
        else if (method == PaymentMethods.Cash) // Hoặc dùng Constant nếu bạn có PaymentMethods.CASH
        {
            // Xử lý thanh toán tiền mặt trực tiếp
            transaction.InternalCode = $"CASH-{order.Code}-{DateTime.UtcNow.Ticks}";
            transaction.TransactionStatus = "PAID"; // Trực tiếp thanh toán xong
            transaction.Note = $"Thanh toán trực tiếp bằng tiền mặt cho đơn hàng {order.Code}";

            // Cập nhật luôn trạng thái Invoice vì đã nhận tiền mặt
            order.Invoice.PaymentStatus = InvoiceStatuses.Paid;

            _context.Transactions.Add(transaction);
            return new { Message = "Thanh toán tiền mặt thành công", OrderCode = order.Code };
        }

        throw new Exception("Phương thức thanh toán không hỗ trợ");
    }
    private bool ProcessOrderWorkflowAfterPayment(Order order)
    {
        // 1. Cập nhật Order sang PROCESSING
        order.OrderStatus = OrderStatuses.Processing;

        // 2. Cập nhật từng OrderItem dựa trên SourceType
        // Vì Checkout đang dùng SourceType cho toàn bộ Request
        foreach (var item in order.OrderItems)
        {
            if (item.SourceType == SourceTypes.InStock)
            {
                // Hàng có sẵn thì chuyển sang "Đang nhặt hàng"
                item.FulfillmentStatus = OrderItemStatuses.Picking;
            }
            if (item.SourceType == SourceTypes.DesignService)
            {
                // Hàng thiết kế/in theo yêu cầu
                item.FulfillmentStatus = OrderItemStatuses.Designing;
            }
            if (item.SourceType == SourceTypes.PreOrder || item.SourceType == SourceTypes.PrintService)
            {
                item.FulfillmentStatus = OrderItemStatuses.Printing;
            }
        }
        return true;
    }
    #endregion
}

