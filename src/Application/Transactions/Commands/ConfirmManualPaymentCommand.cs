using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Transactions.Commands;

[Authorize(Roles = Roles.StaffOrManager)]
public record ConfirmManualPaymentCommand : IRequest<object>
{
    [Required]
    public Guid OrderId { get; init; }

    [Required]
    [DefaultValue(PaymentMethods.Cash)]
    public string PaymentMethod { get; init; } = null!;

    /// <summary>Mã tham chiếu giao dịch ngoài hệ thống (số bill, mã chuyển khoản, v.v.)</summary>
    [DefaultValue(null)]
    public string? ExternalReference { get; init; }

    [DefaultValue(null)]
    public string? Note { get; init; }
}

public class ConfirmManualPaymentCommandHandler : IRequestHandler<ConfirmManualPaymentCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IOrderWorkflowService _orderWorkflowService;
    private readonly IUser _user;

    public ConfirmManualPaymentCommandHandler(
        IApplicationDbContext context,
        IOrderWorkflowService orderWorkflowService,
        IUser user)
    {
        _context = context;
        _orderWorkflowService = orderWorkflowService;
        _user = user;
    }

    public async Task<object> Handle(ConfirmManualPaymentCommand request, CancellationToken cancellationToken)
    {
        // Only Cash and BankTransfer are allowed for manual confirmation
        var allowedMethods = new[] { PaymentMethods.Cash, PaymentMethods.BankTransfer };
        if (!allowedMethods.Contains(request.PaymentMethod))
        {
            throw new BusinessException(
                $"Phương thức '{request.PaymentMethod}' không hỗ trợ xác nhận thủ công. Chỉ chấp nhận CASH hoặc BANK_TRANSFER.",
                ResponseCodeConstants.VAL_INVALID_INPUT);
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.DesignVariant)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.DesignWork)
            .Include(o => o.Customer)
            .Include(o => o.Invoice)
                .ThenInclude(i => i!.Transactions)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new DataNotFoundException(nameof(Order), request.OrderId);

        if (order.OrderStatus != OrderStatuses.Pending)
            throw new BusinessException(
                "Đơn hàng không ở trạng thái chờ thanh toán.",
                ResponseCodeConstants.VAL_INVALID_STATE);

        if (order.Invoice?.PaymentStatus == InvoiceStatuses.Paid)
            throw new BusinessException(
                "Đơn hàng đã được thanh toán trước đó.",
                ResponseCodeConstants.VAL_INVALID_STATE);

        var confirmedBy = _user.Username ?? "SYSTEM_AUTO";
        var now = CoreHelper.SystemTimeNow;

        // Ensure invoice exists
        if (order.Invoice == null)
        {
            order.Invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                InvoiceCode = $"INV-{order.Code}",
                TotalAmount = order.TotalPrice,
                PaymentStatus = InvoiceStatuses.Unpaid,
                Created = now,
                CreatedBy = confirmedBy,
                LastModified = now,
                LastModifiedBy = confirmedBy,
            };
        }

        var noteText = BuildNote(request, order, confirmedBy);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            InvoiceId = order.Invoice.Id,
            Invoice = order.Invoice,
            Amount = order.TotalPrice,
            PaymentMethod = request.PaymentMethod,
            InternalCode = $"MANUAL-{request.PaymentMethod}-{order.Code}-{now.Ticks}",
            ExternalTransactionId = request.ExternalReference,
            TransactionStatus = TransactionStatuses.Success,
            PaidAt = now,
            Note = noteText,
            Created = now,
            CreatedBy = confirmedBy,
            LastModified = now,
            LastModifiedBy = confirmedBy,
        };

        order.Invoice.PaymentStatus = InvoiceStatuses.Paid;
        order.Invoice.LastModified = now;
        order.Invoice.LastModifiedBy = confirmedBy;

        _context.Transactions.Add(transaction);

        await _orderWorkflowService.ActivateOrderWorkflowAsync(order, cancellationToken);
        order.LastModifiedBy = confirmedBy;

        await _context.SaveChangesAsync(cancellationToken);

        return new
        {
            OrderCode = order.Code,
            TransactionId = transaction.Id,
            PaymentMethod = transaction.PaymentMethod,
            Amount = transaction.Amount,
            ExternalReference = transaction.ExternalTransactionId,
            ConfirmedBy = confirmedBy,
            PaidAt = transaction.PaidAt,
        };
    }

    private static string BuildNote(ConfirmManualPaymentCommand request, Order order, string confirmedBy)
    {
        var method = request.PaymentMethod == PaymentMethods.BankTransfer ? "chuyển khoản ngân hàng" : "tiền mặt";
        var refPart = string.IsNullOrWhiteSpace(request.ExternalReference)
            ? string.Empty
            : $" Mã tham chiếu: {request.ExternalReference}.";
        var customNote = string.IsNullOrWhiteSpace(request.Note)
            ? string.Empty
            : $" Ghi chú: {request.Note}";

        return $"Xác nhận thanh toán {method} thủ công cho đơn {order.Code} bởi {confirmedBy}.{refPart}{customNote}";
    }
}
