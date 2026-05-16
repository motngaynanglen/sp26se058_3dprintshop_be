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
using sp26se058_3dprintshop_be.Application.Common.Constants;

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
    private readonly IOrderWorkflowService _orderWorkflowService;
    private readonly IPaymentService _paymentService;
    private readonly PayOsSettings _payOsSettings;
    private readonly IUser _user;

    public PerformTransactionCommandHandler(IApplicationDbContext context, IOrderWorkflowService orderWorkflowService, IPaymentService paymentService, IOptions<PayOsSettings> payOsSettings, IUser user)
    {
        _context = context;
        _orderWorkflowService = orderWorkflowService;
        _paymentService = paymentService;
        _payOsSettings = payOsSettings.Value;
        _user = user;
    }

    public async Task<object> Handle(PerformTransactionCommand request, CancellationToken cancellationToken)
    {
        // 0. Kiểm tra quyền hạn 
        if (request.PaymentMethod == PaymentMethods.Cash && _user.Role == Roles.CUSTOMER)
        {
            throw new ForbiddenAccessException("Khách hàng không được tự xác nhận thanh toán tiền mặt.");
        }

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
            await _orderWorkflowService.ActivateOrderWorkflowAsync(order, cancellationToken);
        }
        // 5. Tạo Transaction mới dựa trên phương thức thanh toán
        var result = await CreateTransactionByMethodAsync(order, request.PaymentMethod, cancellationToken);

        // 6. Lưu thay đổi
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Lỗi khi lưu xuống DB
            throw new UpdateFailureException($"Lỗi lưu dữ liệu giao dịch: {ex.Message}");
        }
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

        if (order == null) throw new DataNotFoundException(nameof(Order), orderId);
        return order;
    }

    private bool ValidateOrderForTransaction(Order order)
    {
        bool isExpired = order.OrderStatus == OrderStatuses.Pending
            && (order.Invoice?.DueDate.HasValue == true
                ? CoreHelper.SystemTimeNow.UtcDateTime > order.Invoice.DueDate.Value
                : CoreHelper.SystemTimeNow > order.Created.AddMinutes(OrderPaymentConstants.PendingPaymentLifetimeMinutes));

        if (isExpired)
        {
            throw new BusinessException(
                "Đơn hàng đã quá hạn thanh toán. Vui lòng tạo đơn hàng mới để tiếp tục.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        bool isNotPending = order.OrderStatus != OrderStatuses.Pending;
        bool isPaid = order.Invoice != null && order.Invoice.PaymentStatus == InvoiceStatuses.Paid;

        if (isNotPending || isPaid)
        {
            // Chuyển sang VAL_002: Vì đơn hàng tồn tại nhưng trạng thái không cho phép tạo giao dịch mới
            throw new BusinessException(
                "Đơn hàng đã được thanh toán hoặc không còn ở trạng thái chờ xử lý.",
                ResponseCodeConstants.VAL_INVALID_STATE
            );
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
                DueDate = CoreHelper.SystemTimeNow.UtcDateTime.AddMinutes(OrderPaymentConstants.PendingPaymentLifetimeMinutes),
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
        var pendingTransaction = invoice.Transactions
            .FirstOrDefault(t => t.TransactionStatus == TransactionStatuses.Pending
                                    && t.PaymentMethod == PaymentMethods.PAYOS);

        if (pendingTransaction == null) return null;

        // Kiểm tra thời hạn 15 phút
        bool isTimeOut = pendingTransaction.Invoice.DueDate.HasValue
            ? CoreHelper.SystemTimeNow.UtcDateTime > pendingTransaction.Invoice.DueDate.Value
            : CoreHelper.SystemTimeNow > pendingTransaction.Created.AddMinutes(OrderPaymentConstants.PendingPaymentLifetimeMinutes);
        if (isTimeOut)
        {
            pendingTransaction.TransactionStatus = TransactionStatuses.Failed;
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
            TransactionStatus = TransactionStatuses.Pending,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username
        };

        if (method == PaymentMethods.PAYOS)
        {
            // Xử lý tạo Link thanh toán online
            var paymentResponse = await _paymentService.CreatePaymentLink(order, _payOsSettings.ReturnUrl, _payOsSettings.CancelUrl);
            if (paymentResponse == null)
            {
                // Chuyển sang EXT_501: Lỗi cổng thanh toán bên thứ 3
                throw new BusinessException(
                    "Không thể kết nối với cổng thanh toán PayOS. Vui lòng thử lại sau.",
                    ResponseCodeConstants.PAYOS_ERROR
                );
            }

            transaction.InternalCode = paymentResponse.PaymentCode.ToString();
            transaction.TransactionStatus = TransactionStatuses.Pending;
            transaction.PaymentLink = paymentResponse.PaymentLink;
            transaction.QrCode = paymentResponse.QrCode;
            transaction.Note = $"Tạo link thanh toán PayOS cho đơn hàng {order.Code}";
            order.Invoice.DueDate = paymentResponse.ExpiredAt.UtcDateTime;

            _context.Transactions.Add(transaction);
            return paymentResponse;
        }
        else if (method == PaymentMethods.Cash) // Hoặc dùng Constant nếu bạn có PaymentMethods.CASH
        {
            // Xử lý thanh toán tiền mặt trực tiếp
            transaction.InternalCode = $"CASH-{order.Code}-{DateTime.UtcNow.Ticks}";
            transaction.TransactionStatus = TransactionStatuses.Success; // Trực tiếp thanh toán xong
            transaction.Note = $"Thanh toán trực tiếp bằng tiền mặt cho đơn hàng {order.Code}";

            // Cập nhật luôn trạng thái Invoice vì đã nhận tiền mặt
            order.Invoice.PaymentStatus = InvoiceStatuses.Paid;

            _context.Transactions.Add(transaction);
            return new { Message = "Thanh toán tiền mặt thành công", OrderCode = order.Code };
        }

        throw new BusinessException(
            $"Phương thức thanh toán '{method}' hiện chưa được hệ thống hỗ trợ.",
            ResponseCodeConstants.NOT_SUPPORTED
        );
    }
    #endregion
}

