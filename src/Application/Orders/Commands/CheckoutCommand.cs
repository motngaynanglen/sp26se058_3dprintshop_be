using System.ComponentModel;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Application.Shipping;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;

/// <summary>
/// [Customer] Tạo đơn hàng — thống nhất 3 luồng:
/// • IN_STOCK / PRE_ORDER → <c>DesignVariantId</c>
/// • CUSTOM_FILE_PRINT_MF2 / CUSTOM_QUOTE_MF2 / AI_GENERATED → <c>DesignWorkId</c>
/// </summary>
public record CheckoutCommand : IRequest<CheckoutResult>
{
    public required Guid ShippingAddressId { get; init; }

    [DefaultValue(0)]
    public decimal ShippingFee { get; init; } = 0;

    /// <summary>GHN / GHTK / MANUAL — ghi nhận lựa chọn khách; vận đơn GHN/GHTK do staff tạo sau.</summary>
    [DefaultValue(ShippingCarriers.Manual)]
    public string? ShippingCarrier { get; init; }

    public string? Note { get; init; }

    public required List<CheckoutItemDto> Items { get; init; } = new();
}

public record CheckoutItemDto
{
    /// <summary>SourceType: IN_STOCK, PRE_ORDER, CUSTOM_FILE_PRINT_MF2, CUSTOM_QUOTE_MF2, AI_GENERATED.</summary>
    public required string SourceType { get; init; }

    /// <summary>Bắt buộc với IN_STOCK / PRE_ORDER.</summary>
    public Guid? DesignVariantId { get; init; }

    /// <summary>Bắt buộc với CUSTOM_FILE_PRINT_MF2 / CUSTOM_QUOTE_MF2 / AI_GENERATED.</summary>
    public Guid? DesignWorkId { get; init; }

    [DefaultValue(1)]
    public int Quantity { get; init; } = 1;
}

public record CheckoutResult(Guid OrderId, string Code, decimal TotalPrice);

public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.ShippingAddressId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Đơn hàng phải có ít nhất 1 sản phẩm.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.SourceType).NotEmpty();
        });
    }
}

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, CheckoutResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IGhnAddressResolver _ghnResolver;
    private readonly IUser _user;

    public CheckoutCommandHandler(
        IApplicationDbContext context,
        IGhnAddressResolver ghnResolver,
        IUser user)
    {
        _context = context;
        _ghnResolver = ghnResolver;
        _user = user;
    }

    public async Task<CheckoutResult> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var accountId = _user.Id.ToGuid();
        var customer = await _context.Customers
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Chỉ khách hàng mới tạo được đơn hàng.");

        var shippingAddress = await _context.ShippingAddresses
            .FirstOrDefaultAsync(s => s.Id == request.ShippingAddressId, cancellationToken)
            ?? throw new InvalidOperationException("Địa chỉ giao hàng không tồn tại.");
        if (shippingAddress.CustomerId != customer.Id)
            throw new UnauthorizedAccessException("Địa chỉ giao hàng không thuộc về bạn.");

        var carrier = string.IsNullOrWhiteSpace(request.ShippingCarrier)
            ? ShippingCarriers.Manual
            : request.ShippingCarrier.Trim().ToUpperInvariant();

        await GhnAddressResolveHelper.EnsureGhnCodesAsync(shippingAddress, _ghnResolver, cancellationToken);

        if (carrier == ShippingCarriers.Ghn
            && (shippingAddress.GhnDistrictId is null or <= 0
                || string.IsNullOrWhiteSpace(shippingAddress.GhnWardCode)))
        {
            throw new InvalidOperationException(
                "Không map được mã GHN từ địa chỉ — vui lòng chọn lại Tỉnh → Quận → Phường GHN khi checkout.");
        }

        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? customer.Account.Username;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = GenerateOrderCode(),
            CustomerId = customer.Id,
            OrderStatus = OrderStatuses.Pending,
            TotalPrice = 0,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        };

        decimal subTotal = 0;

        foreach (var input in request.Items)
        {
            var sourceType = NormalizeSourceType(input.SourceType);

            if (sourceType == SourceTypes.InStock || sourceType == SourceTypes.PreOrder || sourceType == SourceTypes.Order)
            {
                var items = await BuildVariantOrderItemsAsync(order, input, sourceType, username, now, cancellationToken);
                foreach (var item in items)
                {
                    subTotal += item.TotalPrice;
                    order.OrderItems.Add(item);
                }
            }
            else if (SourceTypes.IsCustomPrintFlow(sourceType)
                     || sourceType == SourceTypes.CustomQuoteMainflow2)
            {
                var item = await BuildDesignWorkOrderItemAsync(order, input, sourceType, customer.Id, username, now, cancellationToken);
                subTotal += item.TotalPrice;
                order.OrderItems.Add(item);
            }
            else
            {
                throw new InvalidOperationException($"SourceType không hỗ trợ: {input.SourceType}");
            }
        }

        order.TotalPrice = subTotal + request.ShippingFee;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            InvoiceCode = $"INV-{now:yyyyMMdd}-{order.Code}",
            SubTotal = subTotal,
            ShippingFee = request.ShippingFee,
            TaxAmount = 0,
            TotalAmount = order.TotalPrice,
            PaymentStatus = InvoiceStatuses.Unpaid,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        };
        order.Invoice = invoice;

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ShippingAddressId = shippingAddress.Id,
            ShippingFee = request.ShippingFee,
            Carrier = carrier,
            ShipmentStatus = ShipmentStatuses.Preparing,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        };

        _context.Orders.Add(order);
        _context.Invoices.Add(invoice);
        _context.Shipments.Add(shipment);

        await _context.SaveChangesAsync(cancellationToken);

        return new CheckoutResult(order.Id, order.Code, order.TotalPrice);
    }

    private async Task<List<OrderItem>> BuildVariantOrderItemsAsync(
        Order order,
        CheckoutItemDto input,
        string sourceType,
        string username,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (input.DesignVariantId == null || input.DesignVariantId == Guid.Empty)
            throw new InvalidOperationException("DesignVariantId là bắt buộc cho IN_STOCK / PRE_ORDER.");

        var variant = await _context.DesignVariants
            .Include(v => v.DesignTemplate)
            .FirstOrDefaultAsync(v => v.Id == input.DesignVariantId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sản phẩm.");

        if (!variant.IsActive)
            throw new InvalidOperationException($"Sản phẩm {variant.Name} đã ngừng kinh doanh.");

        var baseName = $"{variant.DesignTemplate.Name} - {variant.Name}";
        var items = new List<OrderItem>();
        var wantsInStock = sourceType is SourceTypes.InStock or SourceTypes.Order;
        var stockAvailable = Math.Max(0, variant.StockQuantity);
        var requestedQty = input.Quantity;

        if (wantsInStock && requestedQty > stockAvailable)
        {
            if (!variant.IsAllowPreOrder)
                throw new InvalidOperationException(
                    $"Sản phẩm {variant.Name} không đủ tồn kho (còn {stockAvailable}).");

            if (stockAvailable > 0)
            {
                items.Add(CreateVariantOrderItem(
                    order, variant, baseName, stockAvailable, SourceTypes.InStock,
                    OrderItemStatuses.Picking, username, now));
                DeductStock(order, variant, stockAvailable, username, now);
            }

            var preOrderQty = requestedQty - stockAvailable;
            items.Add(CreateVariantOrderItem(
                order, variant, $"{baseName} (cần in thêm)", preOrderQty, SourceTypes.PreOrder,
                OrderItemStatuses.Pending, username, now));
        }
        else if (wantsInStock)
        {
            items.Add(CreateVariantOrderItem(
                order, variant, baseName, requestedQty, SourceTypes.InStock,
                OrderItemStatuses.Picking, username, now));
            DeductStock(order, variant, requestedQty, username, now);
        }
        else
        {
            items.Add(CreateVariantOrderItem(
                order, variant, $"{baseName} (cần in thêm)", requestedQty, SourceTypes.PreOrder,
                OrderItemStatuses.Pending, username, now));
        }

        return items;
    }

    private static OrderItem CreateVariantOrderItem(
        Order order,
        Domain.Entities.DesignVariant variant,
        string itemName,
        int quantity,
        string sourceType,
        string fulfillmentStatus,
        string username,
        DateTimeOffset now)
    {
        var unitPrice = variant.Price;
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            SourceType = sourceType,
            DesignVariantId = variant.Id,
            ItemName = itemName,
            QuantityOrdered = quantity,
            UnitPrice = unitPrice,
            TotalPrice = unitPrice * quantity,
            FulfillmentStatus = fulfillmentStatus,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        };
    }

    private void DeductStock(
        Order order,
        Domain.Entities.DesignVariant variant,
        int quantity,
        string username,
        DateTimeOffset now)
    {
        variant.StockQuantity -= quantity;
        variant.LastModified = now;
        variant.LastModifiedBy = username;

        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            DesignVariantId = variant.Id,
            Type = InventoryTransactionTypes.OrderOut,
            Quantity = -quantity,
            ReferenceId = order.Id,
            Note = $"Xuất kho cho đơn {order.Code}",
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        });
    }

    private async Task<OrderItem> BuildDesignWorkOrderItemAsync(
        Order order,
        CheckoutItemDto input,
        string sourceType,
        Guid customerId,
        string username,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (input.DesignWorkId == null || input.DesignWorkId == Guid.Empty)
            throw new InvalidOperationException($"DesignWorkId là bắt buộc cho {sourceType}.");

        var dw = await _context.DesignWorks
            .FirstOrDefaultAsync(d => d.Id == input.DesignWorkId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy DesignWork.");

        if (dw.CustomerId != customerId)
            throw new UnauthorizedAccessException("DesignWork không thuộc về bạn.");

        if (dw.SourceType != sourceType)
            throw new InvalidOperationException($"DesignWork không thuộc loại {sourceType}.");

        if (sourceType is SourceTypes.CustomFilePrintMainflow2
            or SourceTypes.CustomQuoteMainflow2)
        {
            if (dw.Status != Mainflow2DesignWorkStatuses.Approved)
                throw new InvalidOperationException("Bạn cần duyệt báo giá trước khi đặt đơn.");
        }
        else if (OrderPaymentHelper.IsDirectPrintSourceType(sourceType)
                 && dw.Status != Mainflow2DesignWorkStatuses.Approved
                 && dw.LatestQuotedPrice is null or <= 0)
        {
            throw new InvalidOperationException("Đơn in chưa có báo giá — vui lòng duyệt báo giá trước.");
        }

        if (dw.LatestQuotedPrice is null or 0)
            throw new InvalidOperationException("DesignWork chưa có báo giá hợp lệ.");

        var unitPrice = dw.LatestQuotedPrice.Value;
        var totalPrice = unitPrice * input.Quantity;

        var existing = await _context.OrderItems
            .AnyAsync(oi => oi.DesignWorkId == dw.Id
                            && oi.Order.OrderStatus != OrderStatuses.Cancelled, cancellationToken);
        if (existing)
            throw new InvalidOperationException("DesignWork này đã có đơn hàng đang xử lý.");

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            SourceType = sourceType,
            DesignWorkId = dw.Id,
            ItemName = string.IsNullOrWhiteSpace(dw.Name) ? "Sản phẩm thiết kế theo yêu cầu" : dw.Name,
            QuantityOrdered = input.Quantity,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            FulfillmentStatus = OrderItemStatuses.Pending,
            Created = now,
            CreatedBy = username,
            LastModified = now,
            LastModifiedBy = username
        };
    }

    private static string NormalizeSourceType(string raw) => raw?.Trim().ToUpperInvariant() switch
    {
        "IN_STOCK" or "INSTOCK" or "ORDER" => SourceTypes.InStock,
        "PRE_ORDER" or "PREORDER" => SourceTypes.PreOrder,
        "CUSTOM_FILE_PRINT_MF2" or "CUSTOM_FILE_PRINT" => SourceTypes.CustomFilePrintMainflow2,
        "CUSTOM_QUOTE_MF2" or "CUSTOM_QUOTE" => SourceTypes.CustomQuoteMainflow2,
        "AI_GENERATED" or "AI" => SourceTypes.AiGenerated,
        "PRINT_FROM_DESIGN_MF2" or "PRINT_FROM_DESIGN" => SourceTypes.PrintFromDesignMainflow2,
        "REPRINT_MF2" or "REPRINT" => SourceTypes.ReprintMainflow2,
        _ => raw ?? string.Empty
    };

    private static string GenerateOrderCode()
    {
        var now = CoreHelper.SystemTimeNow;
        var rand = Random.Shared.Next(1000, 9999);
        return $"ORD{now:yyMMddHHmmss}{rand}";
    }
}
