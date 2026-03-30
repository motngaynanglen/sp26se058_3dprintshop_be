using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;

[Authorize(Roles = Roles.CUSTOMER)]
public record CheckoutCommand : IRequest<Guid>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid ShippingAddressId { get; init; }
    //public Guid ShippingMethodId { get; init; }
    //[DefaultValue("ONLINE")]
    //public string? PaymentMethod { get; init; } // MoMo, BankTransfer
    public string? Note { get; init; }
    public List<CheckoutItemRequest> Items { get; init; } = new();
}
public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user; 

    public CheckoutCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }
    public async Task<Guid> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid(); // Lấy ID từ Token
        var customer = await _context.Customers.FirstOrDefaultAsync(x=> x.AccountId == userId);
        if(customer == null)
        {
            throw new Exception("Chỉ có khách hàng mới có thể dùng phương thức này.");
        }
        var order = await CreateOrderAsync(customer.Id, request.Items, cancellationToken);
        var shipment = CreateShipment(order.Id, request.ShippingAddressId, 0);
        var invoice = CreateInvoice(order);

        await _context.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
    private async Task<Order> CreateOrderAsync(Guid customerId, List<CheckoutItemRequest> items, CancellationToken ct)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = $"ORD-{DateTime.UtcNow.Ticks}", // Dùng UtcNow cho chuẩn xác
            CustomerId = customerId,
            OrderStatus = "PENDING",
            TotalPrice = 0
        };

        foreach (var itemReq in items)
        {
            decimal unitPrice = 0;
            if (itemReq.SourceType == "ORDER" && itemReq.DesignVariantId.HasValue)
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

                var inventoryLog = new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    DesignVariantId = variant.Id,
                    // Lưu ID của Order để sau này biết món hàng này xuất cho đơn nào
                    ReferenceId = order.Id,
                    Quantity = -itemReq.Quantity, // Xuất kho để số âm
                    Type = "OrderOut", // Khớp với enum/string bạn đã định nghĩa
                    Note = $"Khách hàng đặt hàng từ đơn: {order.Code}",
                    // StaffId = null vì đây là hệ thống tự động xuất khi khách mua
                };

                _context.InventoryTransactions.Add(inventoryLog);

                unitPrice = variant.Price;
            }

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                SourceType = itemReq.SourceType,
                DesignVariantId = itemReq.DesignVariantId,
                QuantityOrdered = itemReq.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * itemReq.Quantity,
                FulfillmentStatus = "PENDING",
                Order = order
            };
            order.TotalPrice += orderItem.TotalPrice;
            _context.OrderItems.Add(orderItem);
        }
        _context.Orders.Add(order);
        return order;
    }
    private Task<Shipment> CreateShipment(Guid orderId, Guid addressId, decimal fee)
    {
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ShippingAddressId = addressId,
            ShippingFee = fee,
            ShipmentStatus = "PENDING"
        };

        _context.Shipments.Add(shipment);

        return Task.FromResult(shipment);
    }
    private Task<Invoice> CreateInvoice(Order order)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            InvoiceCode = $"INV-{DateTime.UtcNow.Ticks}-{order.Code}",
            TotalAmount = order.TotalPrice,
            PaymentStatus = "UNPAID",
        };
        _context.Invoices.Add(invoice);
        return Task.FromResult(invoice);
    }
    //public async Task<Guid> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    //{
    //    var customerId = _user.Id.ToGuid(); // Lấy ID từ Token
    //    //using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    //    var order = new Order
    //    {
    //        Id = Guid.NewGuid(),
    //        Code = $"ORD-{DateTime.Now.Ticks}",// Chưa impliment code
    //        CustomerId = customerId,
    //        OrderStatus = "PENDING",
    //        Priority = 0,
    //        TotalPrice = 0 // Sẽ cộng dồn sau
    //    };
    //    decimal totalAmount = 0;
    //    // 3. Xử lý Order Items
    //    foreach (var itemReq in request.Items)
    //    {
    //        decimal unitPrice = 0;

    //        // Logic lấy giá tùy theo loại hàng
    //        if (itemReq.SourceType == "ORDER" && itemReq.DesignVariantId.HasValue)
    //        {
    //            var variant = await _context.DesignVariants
    //                .FindAsync(new object[] { itemReq.DesignVariantId.Value }, cancellationToken);

    //            if (variant == null || variant.StockQuantity < itemReq.Quantity && !variant.IsAllowPreOrder)
    //                throw new Exception($"Sản phẩm {variant?.Name} không đủ hàng.");

    //            unitPrice = variant.Price;
    //        }
    //        //else if (itemReq.SourceType == "DesignService" && itemReq.DesignWorkId.HasValue)
    //        //{
    //        //    // Logic lấy giá từ gói thiết kế (ServicePackage) liên quan
    //        //    var designWork = await _context.DesignWorks.FindAsync(itemReq.DesignWorkId.Value);
    //        //    // Giả sử unitPrice lấy từ gói dịch vụ...
    //        //}

    //        var orderItem = new OrderItem
    //        {
    //            Id = Guid.NewGuid(),
    //            //OrderId = order.Id,
    //            SourceType = itemReq.SourceType,
    //            DesignVariantId = itemReq.DesignVariantId,
    //            //DesignWorkId = itemReq.DesignWorkId,
    //            QuantityOrdered = itemReq.Quantity,
    //            UnitPrice = unitPrice,
    //            TotalPrice = unitPrice * itemReq.Quantity,
    //            FulfillmentStatus = "Pending",

    //            Order = order
    //        };

    //        totalAmount += orderItem.TotalPrice;
    //        _context.OrderItems.Add(orderItem);
    //        // 4. Tính toán phí Ship & Tổng tiền
    //        //var shippingMethod = await _context.ShippingMethods.FindAsync(request.ShippingMethodId);
    //        //decimal shippingFee = shippingMethod?.BaseFee ?? 0; // Giả định có BaseFee

    //        //order.TotalPrice = totalAmount + shippingFee;
    //        //// Quy tắc: Cọc 50%
    //        //order.DepositAmount = order.TotalPrice * 0.5m;
    //        //order.PaidAmount = 0;

    //        _context.Orders.Add(order);

    //        // 5. Tạo Shipment
    //        var shipment = new Shipment
    //        {
    //            Id = Guid.NewGuid(),
    //            OrderId = order.Id,
    //            ShippingAddressId = request.ShippingAddressId,
    //            //ShippingMethodId = request.ShippingMethodId,
    //            ShippingFee = 0,
    //            ShipmentStatus = "PENDING"
    //        };
    //        _context.Shipments.Add(shipment);
    //        // 6. Tạo Invoice
    //        var invoice = new Invoice
    //        {
    //            Id = Guid.NewGuid(),
    //            OrderId = order.Id,
    //            InvoiceCode = $"INV-{order.Id}",
    //            TotalAmount = order.TotalPrice,
    //            PaymentStatus = "UNPAID",
    //        };
    //        _context.Invoices.Add(invoice);
    //        await _context.SaveChangesAsync(cancellationToken);
    //        return order.Id;
    //    }
    //    //await transaction.RollbackAsync(cancellationToken);
    //    throw new Exception("Lỗi add");
    //}
}
public record CheckoutItemRequest
{
    [DefaultValue("ORDER")]
    public string SourceType { get; init; } = null!;
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid? DesignVariantId { get; init; }
    //public Guid? DesignWorkId { get; init; }
    public int Quantity { get; init; }
}
