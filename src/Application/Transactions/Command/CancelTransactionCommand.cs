using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Transactions.Command;
[Authorize(Roles = Roles.CUSTOMER + "," + Roles.STAFF + "," + Roles.MANAGER)]

public record CancelTransactionCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid TransactionId { get; init; }
    [DefaultValue("Tôi thích thì tôi hủy đơn thôi.")]
    public string? Reason { get; init; }
}
public class CancelTransactionCommandHandler : IRequestHandler<CancelTransactionCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IUser _user;

    public CancelTransactionCommandHandler(IApplicationDbContext context, IPaymentService paymentService, IUser user)
    {
        _context = context;
        _paymentService = paymentService;
        _user = user;
    }

    public async Task<bool> Handle(CancelTransactionCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm giao dịch (Dùng DataNotFoundException - DB_004)
        var transaction = await _context.Transactions
            .Include(t => t.Invoice)
                .ThenInclude(i => i.Order)
                    .ThenInclude(o => o.Customer)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (transaction == null)
        {
            throw new DataNotFoundException(nameof(Transaction), request.TransactionId);
        }

        ValidateCancelPermission(transaction);

        if (transaction.Invoice.PaymentStatus != InvoiceStatuses.Unpaid)
        {
            throw new BusinessException(
                "Chỉ có thể hủy giao dịch khi hóa đơn chưa thanh toán.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }
        // 2. Kiểm tra trạng thái giao dịch (Dùng VAL_002 - Sai trạng thái nghiệp vụ)
        // Nếu đã CANCELLED rồi thì không báo lỗi (Idempotent), nhưng nếu đã Success thì KHÔNG được hủy.
        if (transaction.TransactionStatus == TransactionStatuses.Success)
        {
            throw new BusinessException(
                "Giao dịch đã được thanh toán thành công, không thể hủy.",
                ResponseCodeConstants.VAL_INVALID_STATE
            );
        }
        // Nếu đã hủy từ trước, trả về true để báo hành động đã hoàn tất (Idempotency)
        if (transaction.TransactionStatus == TransactionStatuses.Cancelled)
        {
            throw new BusinessException(
                "Giao dịch này đã được hủy từ trước đó.",
                ResponseCodeConstants.VAL_INVALID_STATE
            );
        }

        // 1. Xử lý hủy trên cổng thanh toán (PayOS)
        if (transaction.PaymentMethod == PaymentMethods.PAYOS && !string.IsNullOrEmpty(transaction.InternalCode))
        {
            try
            {
                await _paymentService.CancelPaymentLink(transaction.InternalCode.ToLong(), request.Reason ?? "Hủy giao dịch");
            }
            catch (Exception ex)
            {
                // Đối với PayOS, nếu hủy thất bại có thể do link đã hết hạn hoặc đã hủy bên PayOS rồi
                // Log lỗi PayOS nhưng vẫn tiếp tục cập nhật DB của mình
                Console.WriteLine($"PayOS Cancel Error: {ex.Message}");
            }
        }

        // 2. Cập nhật trạng thái trong Database
        transaction.TransactionStatus = TransactionStatuses.Cancelled;
        transaction.Note = $"[Hủy bởi {_user.Username}]: {request.Reason ?? "Không có lý do"}.";
        transaction.LastModified = CoreHelper.SystemTimeNow;
        transaction.LastModifiedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException(ex.Message);
        }

        return true;
    }

    private void ValidateCancelPermission(Transaction transaction)
    {
        var role = _user.Role ?? Roles.GUEST;
        var userId = _user.Id.ToGuid();
        var isStaffOrManager = role == Roles.STAFF || role == Roles.MANAGER;

        if (isStaffOrManager)
        {
            return;
        }

        if (role != Roles.CUSTOMER)
        {
            throw new ForbiddenAccessException("Bạn không có quyền hủy giao dịch này.");
        }

        if (transaction.Invoice.Order.Customer.AccountId != userId)
        {
            throw new ForbiddenAccessException("Bạn không có quyền hủy giao dịch của đơn hàng này.");
        }
    }
}
