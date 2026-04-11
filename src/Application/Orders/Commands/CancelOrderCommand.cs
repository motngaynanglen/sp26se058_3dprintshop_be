using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PayOS.Models.V1.Payouts;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;
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
        // 1. Tìm đơn hàng kèm theo các OrderItems và InventoryTransactions liên quan
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Invoice)
                .ThenInclude(i => i!.Transactions)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null) throw new Exception("Không tìm thấy đơn hàng.");

        // 2. Lấy Invoice để kiểm tra trạng thái thanh toán
        var invoice = order.Invoice;
        // 3. LOGIC HỦY: Cho phép hủy nếu Invoice chưa thanh toán (UNPAID) 
        // Hoặc trạng thái đơn hàng vẫn đang ở PENDING
        bool isUnpaid = invoice == null || invoice.PaymentStatus == InvoiceStatuses.Unpaid;
        bool isPending = order.OrderStatus == OrderStatuses.Pending;

        if (invoice == null) throw new Exception("Có lỗi xảy ra khi tạo đơn hàng, không tìm thấy hóa đơn.");
        var payTransaction = invoice.Transactions
                            .FirstOrDefault(t => t.TransactionStatus == "PENDING" && t.Created.AddMinutes(10) > CoreHelper.SystemTimeNow);
        if (payTransaction != null && payTransaction.PaymentMethod == PaymentMethods.PAYOS)
        {
            try
            {
                // Gọi API PayOS để hủy link thanh toán (Tránh khách quét mã sau khi hủy đơn)
                // ExternalTransactionId ở đây thường là OrderCode bạn gửi sang PayOS (ví dụ: 123456)
                await _paymentService.CancelPaymentLink(payTransaction.InternalCode.ToLong(), "Chủ động hủy đơn");
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng có thể cho phép tiếp tục hủy đơn ở DB nếu link PayOS đã hết hạn hoặc đã hủy trước đó
                Console.WriteLine($"PayOS Cancel Error: {ex.Message}");
            }
        }
        if (!isUnpaid && !isPending)
        {
            throw new Exception("Đơn hàng đã thanh toán hoặc đang trong quá trình xử lý, không thể hủy.");
        }

        // 4. Kiểm tra quyền (Chỉ chủ đơn hàng hoặc Admin/Staff mới được hủy)
        // Lưu ý: Bách cần kiểm tra xem userId từ token có khớp với Customer của đơn hàng không
        var userId = _user.Id.ToGuid();
        var role = _user.Role ?? Roles.GUEST;
        var account = await _context.Accounts
                            .Include(a => a.Customer)
                            .Include(a => a.Staff)
                            .Include(a => a.Manager)
                            .FirstOrDefaultAsync(x => x.Id == userId);
        if (role == Roles.GUEST || role == Roles.ADMIN)
        {
            throw new Exception(role + " không thể dùng phương thức này.");
        }
        if (account == null) throw new Exception("Bạn phải đăng nhập để dùng phương thức này.");
        if (role == Roles.CUSTOMER && account.Customer!.Id != order.CustomerId)
        {
            throw new Exception("Chỉ có chủ nhân của đơn hàng mới có thể hủy đơn.");
        }


        // 5. Hoàn trả kho (Inventory Rollback)
        foreach (var item in order.OrderItems)
        {
            item.FulfillmentStatus = "CANCELLED";
            item.LastModified = CoreHelper.SystemTimeNow;
            item.LastModifiedBy = _user.Username;
            // Chỉ hoàn kho đối với loại hàng có sẵn (ORDER) có DesignVariantId
            if (item.SourceType == "ORDER" && item.DesignVariantId.HasValue)
            {
                var variant = await _context.DesignVariants
                    .FirstOrDefaultAsync(v => v.Id == item.DesignVariantId, cancellationToken);

                if (variant != null)
                {
                    // Cộng lại số lượng vào kho
                    variant.StockQuantity += item.QuantityOrdered;

                    // Ghi log nhập lại kho do hủy đơn
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        Id = Guid.NewGuid(),
                        DesignVariantId = variant.Id,
                        ReferenceId = order.Id,
                        Quantity = item.QuantityOrdered, // Số dương vì là nhập lại
                        Type = "OrderCancelReturn",
                        Note = $"Hoàn kho do hủy đơn: {order.Code}. Lý do: {request.Reason}"
                    });
                }
            }
        }

        // 6. Cập nhật trạng thái Đơn hàng, Shipment và Invoice
        order.OrderStatus = "CANCELLED";
        order.LastModified = CoreHelper.SystemTimeNow;
        order.LastModifiedBy = _user.Username;
        //order.Note = $"[Hủy đơn - {DateTime.Now:dd/MM/yyyy HH:mm}] Lý do: {request.Reason}. " + (order.Note ?? "");

        // Cập nhật Shipment
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.OrderId == order.Id, cancellationToken);
        if (shipment != null)
        {
            shipment.ShipmentStatus = "CANCELLED";
            shipment.LastModified = CoreHelper.SystemTimeNow;
            shipment.LastModifiedBy = _user.Username;
        }

        // Cập nhật Invoice
        if (invoice != null)
        {
            invoice.PaymentStatus = "CANCELLED";
            invoice.LastModified = CoreHelper.SystemTimeNow;
            invoice.LastModifiedBy = _user.Username;
        }

        // 6. Lưu thay đổi

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
