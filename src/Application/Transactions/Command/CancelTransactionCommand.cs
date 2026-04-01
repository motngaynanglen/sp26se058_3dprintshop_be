using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Transactions.Command;
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
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (transaction == null) return false;

        // Nếu đã hủy hoặc đã thanh toán thì không làm gì cả
        if (transaction.TransactionStatus != "PENDING") return true;

        // 1. Xử lý hủy trên cổng thanh toán (PayOS)
        if (transaction.PaymentMethod == PaymentMethods.PAYOS && !string.IsNullOrEmpty(transaction.InternalCode))
        {
            try
            {
                await _paymentService.CancelPaymentLink(transaction.InternalCode.ToLong(), request.Reason ?? "Hủy giao dịch");
            }
            catch (Exception ex)
            {
                // Log lỗi PayOS nhưng vẫn tiếp tục cập nhật DB của mình
                Console.WriteLine($"PayOS Cancel Error: {ex.Message}");
            }
        }

        // 2. Cập nhật trạng thái trong Database
        transaction.TransactionStatus = "CANCELLED";
        transaction.Note = $"[Hủy giao dịch] {request.Reason}. " + (transaction.Note ?? "");
        transaction.LastModified = CoreHelper.SystemTimeNow;
        transaction.LastModifiedBy = _user.Username ?? "SYSTEM";

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
