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
    public Guid ShippingAddressId { get; init; }
    //public Guid ShippingMethodId { get; init; }
    [DefaultValue("PAYOS")]
    public string? PaymentMethod { get; init; } // MoMo, BankTransfer
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
        var customerId = _user.Id; // Lấy ID từ Token
        //using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = $"ORD-{DateTime.Now.Ticks}",// Chưa impliment code
            CustomerId = Guid.Parse(customerId!),
            OrderStatus = "PENDING",
            Priority = 0,
            TotalPrice = 0 // Sẽ cộng dồn sau
        };
        decimal totalAmount = 0;
        // 3. Xử lý Order Items
        foreach (var itemReq in request.Items)
        {
            decimal unitPrice = 0;

            // Logic lấy giá tùy theo loại hàng
            if (itemReq.SourceType == "ORDER" && itemReq.DesignVariantId.HasValue)
            {
                var variant = await _context.DesignVariants
                    .FindAsync(new object[] { itemReq.DesignVariantId.Value }, cancellationToken);

                if (variant == null || variant.StockQuantity < itemReq.Quantity && !variant.IsAllowPreOrder)
                    throw new Exception($"Sản phẩm {variant?.Name} không đủ hàng.");

                unitPrice = variant.Price;
            }
            else if (itemReq.SourceType == "DesignService" && itemReq.DesignWorkId.HasValue)
            {
                // Logic lấy giá từ gói thiết kế (ServicePackage) liên quan
                var designWork = await _context.DesignWorks.FindAsync(itemReq.DesignWorkId.Value);
                // Giả sử unitPrice lấy từ gói dịch vụ...
            }

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                //OrderId = order.Id,
                SourceType = itemReq.SourceType,
                DesignVariantId = itemReq.DesignVariantId,
                DesignWorkId = itemReq.DesignWorkId,
                QuantityOrdered = itemReq.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * itemReq.Quantity,
                FulfillmentStatus = "Pending",

                Order = order
            };

            totalAmount += orderItem.TotalPrice;
            _context.OrderItems.Add(orderItem);
            // 4. Tính toán phí Ship & Tổng tiền
            //var shippingMethod = await _context.ShippingMethods.FindAsync(request.ShippingMethodId);
            //decimal shippingFee = shippingMethod?.BaseFee ?? 0; // Giả định có BaseFee

            //order.TotalPrice = totalAmount + shippingFee;
            //// Quy tắc: Cọc 50%
            //order.DepositAmount = order.TotalPrice * 0.5m;
            //order.PaidAmount = 0;

            _context.Orders.Add(order);

            // 5. Tạo Shipment
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ShippingAddressId = request.ShippingAddressId,
                //ShippingMethodId = request.ShippingMethodId,
                ShippingFee = 0,
                ShipmentStatus = "PENDING"
            };
            _context.Shipments.Add(shipment);
            // 6. Tạo Invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                InvoiceCode = $"INV-{order.Id}",
                TotalAmount = order.TotalPrice,
                PaymentStatus = "UNPAID",
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync(cancellationToken);
            return order.Id;
        }
        //await transaction.RollbackAsync(cancellationToken);
        throw new Exception("Lỗi add");
    }
}
public record CheckoutItemRequest
{
    [DefaultValue("ORDER")]
    public string SourceType { get; init; } = null!;
    public Guid? DesignVariantId { get; init; }
    public Guid? DesignWorkId { get; init; }
    public int Quantity { get; init; }
}
