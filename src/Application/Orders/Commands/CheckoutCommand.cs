using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Ocsp;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;

[Authorize(Roles = Roles.CUSTOMER)]
public record CheckoutCommand : IRequest<object>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid ShippingAddressId { get; init; }
    //public Guid ShippingMethodId { get; init; }
    //[DefaultValue("ONLINE")]
    //public string? PaymentMethod { get; init; } // MoMo, BankTransfer
    [DefaultValue(SourceTypes.InStock)]
    public string SourceType { get; init; } = null!;
    public string? Note { get; init; }
    public List<CheckoutItemRequest> Items { get; init; } = new();
}
public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CheckoutCommandHandler(IApplicationDbContext context,IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }
    public async Task<object> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid(); // Lấy ID từ Token
        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.AccountId == userId);
        if (customer == null)
        {
            throw new Exception("Chỉ có khách hàng mới có thể dùng phương thức này.");
        }
        var order = await CreateOrderAsync(customer.Id, request, cancellationToken);
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            Order = order, // Quan trọng: Gán Object
            ShippingAddressId = request.ShippingAddressId,
            ShippingFee = 0,
            ShipmentStatus = ShipmentStatuses.Preparing,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Order = order, // Quan trọng: Gán Object
            InvoiceCode = $"INV-{DateTime.UtcNow.Ticks}",
            TotalAmount = order.TotalPrice,
            PaymentStatus = InvoiceStatuses.Unpaid,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };

        _context.Orders.Add(order);
        _context.Shipments.Add(shipment);
        _context.Invoices.Add(invoice);
        try {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Xem cái này trong Watch window: ex.InnerException.Message
            var message = ex.InnerException?.Message;
            throw new Exception($"Lỗi DB: {message}");
        }

        return _mapper.Map<OrderDTO>(order);
    }
    private async Task<Order> CreateOrderAsync(Guid customerId, CheckoutCommand request, CancellationToken ct)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = $"ORD-{DateTime.UtcNow.Ticks}", // Dùng UtcNow cho chuẩn xác
            CustomerId = customerId,
            OrderStatus = OrderStatuses.Pending,
            TotalPrice = 0,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
            OrderItems = new List<OrderItem>()
        };

        foreach (var itemReq in request.Items)
        {
            var itemName = "Sản phẩm";
            decimal unitPrice = 0;
            if (request.SourceType == SourceTypes.InStock && itemReq.DesignVariantId.HasValue)
            {
                // 1. Lấy thông tin biến thể sản phẩm kèm theo Lock (nếu cần)
                var variant = await _context.DesignVariants
                    .FirstOrDefaultAsync(x => x.Id == itemReq.DesignVariantId.Value, ct);

                if (variant == null)
                    throw new Exception("Sản phẩm không tồn tại.");
                //var variant = await _context.DesignVariants.FindAsync(itemReq.DesignVariantId.Value);
                //if (variant == null) throw new Exception("Biến thể không tồn tại.");
                //unitPrice = variant.Price;

                // 2. Kiểm tra tồn kho
                if (variant.StockQuantity < itemReq.Quantity && !variant.IsAllowPreOrder)
                {
                    throw new Exception($"Sản phẩm '{variant.Name}' hiện chỉ còn {variant.StockQuantity} món, không đủ để đáp ứng yêu cầu của bạn.");
                }

                // 3. CẬP NHẬT INVENTORY: Trừ số lượng trong kho
                // Nếu là hàng Pre-order mà hết kho thì số lượng có thể âm (tùy nghiệp vụ của Bách)
                variant.StockQuantity -= itemReq.Quantity;
                itemName = variant.Name;
                var inventoryLog = new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    DesignVariantId = variant.Id,
                    // Lưu ID của Order để sau này biết món hàng này xuất cho đơn nào
                    ReferenceId = order.Id,
                    Quantity = -itemReq.Quantity, // Xuất kho để số âm
                    Type = InventoryTransactionTypes.OrderOut,
                    Note = $"Khách hàng đặt hàng từ đơn: {order.Code}",
                    // StaffId = null vì đây là hệ thống tự động xuất khi khách mua
                    Created = CoreHelper.SystemTimeNow,
                    CreatedBy = _user.Username,
                    LastModified = CoreHelper.SystemTimeNow,
                    LastModifiedBy = _user.Username
                };

                _context.InventoryTransactions.Add(inventoryLog);

                unitPrice = variant.Price;
            }

            var orderItem = new OrderItem
            {
                //Id = Guid.NewGuid(),
                SourceType = request.SourceType,
                DesignVariantId = itemReq.DesignVariantId,
                QuantityOrdered = itemReq.Quantity,
                ItemName = itemName,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * itemReq.Quantity,
                FulfillmentStatus = OrderItemStatuses.Pending,
                Order = order,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username,
                LastModified = CoreHelper.SystemTimeNow,
                LastModifiedBy = _user.Username
            };
            order.OrderItems.Add(orderItem);
            order.TotalPrice += orderItem.TotalPrice;
        }
        return order;
    }

}
public record CheckoutItemRequest
{

    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid? DesignVariantId { get; init; }
    //public Guid? DesignWorkId { get; init; }
    public int Quantity { get; init; }
}
