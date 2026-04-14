using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PayOS.Models.V1.Payouts;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;
[Authorize(Roles = Roles.CUSTOMER + "," + Roles.STAFF + "," + Roles.MANAGER)]
public record CancelOrderCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid OrderId { get; init; }
    [DefaultValue("Tôi thích thì tôi hủy đơn thôi.")]
    public string? Reason { get; init; } // Lý do hủy đơn
}
public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IUser _user;

    public CancelOrderCommandHandler(IApplicationDbContext context, IUser user, IPaymentService paymentService)
    {
        _context = context;
        _user = user;
        _paymentService = paymentService;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        var userId = _user.Id.ToGuid();
        var role = _user.Role ?? Roles.GUEST;
        // 1. Tìm đơn hàng kèm theo các OrderItems và InventoryTransactions liên quan
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Include(o => o.Invoice)
                .ThenInclude(i => i!.Transactions)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null) throw new DataNotFoundException(nameof(Order), request.OrderId);

        if (order.Invoice == null)
            throw new DataNotFoundException("Không tìm thấy hóa đơn liên quan đến đơn hàng này.");

        if (role == Roles.GUEST || role == Roles.ADMIN)
            throw new ForbiddenAccessException($"{role} không có quyền thực hiện chức năng hủy đơn.");

        if (role == Roles.CUSTOMER && order.Customer?.AccountId != userId)
            throw new ForbiddenAccessException("Bạn không có quyền hủy đơn hàng của người khác.");

        if (order.OrderStatus == OrderStatuses.Cancelled)
            throw new BusinessException("Đơn hàng này đã được hủy trước đó.", ResponseCodeConstants.VAL_INVALID_STATE);

        // 2. Lấy Invoice để kiểm tra trạng thái thanh toán
        var invoice = order.Invoice;
        // 3. LOGIC HỦY: Cho phép hủy nếu Invoice chưa thanh toán (UNPAID) 
        // Hoặc trạng thái đơn hàng vẫn đang ở PENDING
        bool isUnpaid = invoice.PaymentStatus == InvoiceStatuses.Unpaid;
        bool isPending = order.OrderStatus == OrderStatuses.Pending;

        if (!isUnpaid && !isPending)
        {
            failures.Add(new ValidationFailure(nameof(order.OrderStatus),
                "Đơn hàng đã thanh toán hoặc đang xử lý, không thể hủy."));
        }
        failures.ThrowIfAny();

        var payTransaction = invoice.Transactions
                            .FirstOrDefault(t => t.TransactionStatus == TransactionStatuses.Pending
                                            && t.Created.AddMinutes(10) > CoreHelper.SystemTimeNow);
        if (payTransaction != null && payTransaction.PaymentMethod == PaymentMethods.PAYOS)
        {
            try
            {
                // Gọi API PayOS để hủy link thanh toán (Tránh khách quét mã sau khi hủy đơn)
                await _paymentService.CancelPaymentLink(payTransaction.InternalCode.ToLong(), "Chủ động hủy đơn");
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng có thể cho phép tiếp tục hủy đơn ở DB nếu link PayOS đã hết hạn hoặc đã hủy trước đó
                Console.WriteLine($"PayOS Cancel Error: {ex.Message}");
            }
        }

        // 5. Hoàn trả kho (Inventory Rollback)
        var variantIdsToReturn = order.OrderItems
            .Where(i => i.SourceType == SourceTypes.InStock && i.DesignVariantId.HasValue)
            .Select(i => i.DesignVariantId!.Value)
            .ToList();

        if (variantIdsToReturn.Any())
        {
            var variants = await _context.DesignVariants
                .Where(v => variantIdsToReturn.Contains(v.Id))
                .ToListAsync(cancellationToken);
            foreach (var item in order.OrderItems)
            {
                item.FulfillmentStatus = OrderItemStatuses.Cancelled;
                item.LastModified = CoreHelper.SystemTimeNow;
                item.LastModifiedBy = _user.Username;

                if (item.SourceType == SourceTypes.InStock && item.DesignVariantId.HasValue)
                {
                    var variant = variants.FirstOrDefault(v => v.Id == item.DesignVariantId);
                    if (variant != null)
                    {
                        variant.StockQuantity += item.QuantityOrdered;

                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            Id = Guid.NewGuid(),
                            DesignVariantId = variant.Id,
                            ReferenceId = order.Id,
                            Quantity = item.QuantityOrdered,
                            Type = InventoryTransactionTypes.OrderCancelReturn,
                            Note = $"Hoàn kho do hủy đơn {order.Code}. Lý do: {request.Reason}",
                            Created = CoreHelper.SystemTimeNow,
                            CreatedBy = _user.Username
                        });
                    }
                }
            }
        }

        // 6. Cập nhật trạng thái Đơn hàng, Shipment và Invoice
        order.OrderStatus = OrderStatuses.Cancelled;
        order.LastModified = CoreHelper.SystemTimeNow;
        order.LastModifiedBy = _user.Username;
        //order.Note = $"[Hủy đơn - {DateTime.Now:dd/MM/yyyy HH:mm}] Lý do: {request.Reason}. " + (order.Note ?? "");

        // Cập nhật Invoice

        invoice.PaymentStatus = InvoiceStatuses.Cancelled;
        invoice.LastModified = CoreHelper.SystemTimeNow;
        invoice.LastModifiedBy = _user.Username;


        // Cập nhật Shipment
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.OrderId == order.Id, cancellationToken);
        if (shipment != null)
        {
            shipment.ShipmentStatus = ShipmentStatuses.Returned;
            shipment.LastModified = CoreHelper.SystemTimeNow;
            shipment.LastModifiedBy = _user.Username;
        }

        // 6. Lưu thay đổi

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException($"Lỗi khi thực hiện hủy đơn hàng: {ex.InnerException?.Message ?? ex.Message}");
        }

        return true;
    }
}
