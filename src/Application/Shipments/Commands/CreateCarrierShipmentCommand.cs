using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Shipping;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

/// <summary>[Staff/Manager] Tạo vận đơn GHN hoặc GHTK cho đơn hàng đã sẵn sàng giao.</summary>
public record CreateCarrierShipmentCommand : IRequest<CreateCarrierShipmentResult>
{
    public Guid OrderId { get; init; }

    /// <summary>GHN hoặc GHTK — <see cref="ShippingCarriers"/>.</summary>
    public required string Carrier { get; init; }

    public int? WeightGrams { get; init; }

    /// <summary>Bổ sung mã quận GHN khi địa chỉ đơn thiếu.</summary>
    public int? GhnDistrictId { get; init; }

    /// <summary>Bổ sung mã phường GHN khi địa chỉ đơn thiếu.</summary>
    public string? GhnWardCode { get; init; }
}

public record CreateCarrierShipmentResult(
    Guid ShipmentId,
    string Carrier,
    string? CarrierOrderCode,
    string? TrackingNumber,
    string ShipmentStatus);

public class CreateCarrierShipmentCommandValidator : AbstractValidator<CreateCarrierShipmentCommand>
{
    public CreateCarrierShipmentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Carrier)
            .NotEmpty()
            .Must(c => ShippingCarriers.ThirdParty.Contains(c))
            .WithMessage("Carrier phải là GHN hoặc GHTK.");
        When(x => x.WeightGrams.HasValue, () =>
            RuleFor(x => x.WeightGrams!.Value).GreaterThan(0));
    }
}

public class CreateCarrierShipmentCommandHandler
    : IRequestHandler<CreateCarrierShipmentCommand, CreateCarrierShipmentResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IShippingCarrierResolver _carriers;
    private readonly IGhnAddressResolver _ghnResolver;
    private readonly IUser _user;

    public CreateCarrierShipmentCommandHandler(
        IApplicationDbContext context,
        IShippingCarrierResolver carriers,
        IGhnAddressResolver ghnResolver,
        IUser user)
    {
        _context = context;
        _carriers = carriers;
        _ghnResolver = ghnResolver;
        _user = user;
    }

    public async Task<CreateCarrierShipmentResult> Handle(
        CreateCarrierShipmentCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        var role = _user.Role ?? Roles.GUEST;
        if (role != Roles.STAFF && role != Roles.MANAGER && role != Roles.ADMIN)
            throw new UnauthorizedAccessException("Chỉ nhân viên/quản lý tạo được vận đơn GHN/GHTK.");

        var carrierCode = request.Carrier.Trim().ToUpperInvariant();
        var service = _carriers.GetService(carrierCode)
            ?? throw new InvalidOperationException($"Không hỗ trợ đơn vị vận chuyển: {request.Carrier}");

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Invoice)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        if (order.OrderStatus == OrderStatuses.Cancelled)
            throw new InvalidOperationException("Đơn hàng đã hủy.");

        if (order.OrderStatus != OrderStatuses.Processing
            && order.OrderStatus != OrderStatuses.Finished)
            throw new InvalidOperationException(
                "Chỉ tạo vận đơn khi đơn ở trạng thái PROCESSING hoặc FINISHED.");

        var shipment = await _context.Shipments
            .Include(s => s.ShippingAddress)
            .FirstOrDefaultAsync(s => s.OrderId == order.Id, cancellationToken)
            ?? throw new InvalidOperationException("Đơn hàng chưa có shipment.");

        if (!string.IsNullOrWhiteSpace(shipment.CarrierOrderCode))
            throw new InvalidOperationException(
                $"Đã có vận đơn {shipment.Carrier} — mã {shipment.CarrierOrderCode}.");

        var addr = shipment.ShippingAddress;
        if (request.GhnDistrictId is > 0 && !string.IsNullOrWhiteSpace(request.GhnWardCode))
        {
            addr.GhnDistrictId = request.GhnDistrictId;
            addr.GhnWardCode = request.GhnWardCode.Trim();
        }

        await GhnAddressResolveHelper.EnsureGhnCodesAsync(addr, _ghnResolver, cancellationToken);

        if (carrierCode == ShippingCarriers.Ghn
            && (addr.GhnDistrictId is null or <= 0 || string.IsNullOrWhiteSpace(addr.GhnWardCode)))
        {
            throw new InvalidOperationException(
                $"Không map được mã GHN từ địa chỉ «{addr.Ward}, {addr.District}, {addr.City}». "
                + "Kiểm tra tên Phường/Quận/Tỉnh khớp danh mục GHN hoặc chọn lại trên form.");
        }

        var weight = request.WeightGrams ?? 500;
        var isPaid = order.Invoice?.PaymentStatus == InvoiceStatuses.Paid;

        var items = order.OrderItems.Select(oi => new CarrierShipmentLineItem(
            oi.ItemName ?? "Sản phẩm",
            oi.QuantityOrdered,
            Math.Max(0.2m, weight / 1000m / Math.Max(1, order.OrderItems.Count)))).ToList();

        if (items.Count == 0)
            items.Add(new CarrierShipmentLineItem("3D Print", 1, weight / 1000m));

        var createCtx = new CarrierShipmentCreateContext(
            order.Id,
            order.Code,
            order.TotalPrice,
            isPaid,
            addr.ReceiverName,
            addr.Phone,
            addr.AddressLine,
            addr.Ward,
            addr.District,
            addr.City,
            addr.Province,
            weight,
            items,
            addr.GhnDistrictId,
            addr.GhnWardCode);

        var apiResult = await service.CreateShipmentAsync(createCtx, cancellationToken);
        if (!apiResult.Success)
            throw new InvalidOperationException(apiResult.ErrorMessage ?? "Tạo vận đơn thất bại.");

        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? "staff";

        shipment.Carrier = carrierCode;
        shipment.CarrierOrderCode = apiResult.CarrierOrderCode;
        shipment.CarrierStatus = apiResult.CarrierStatus;
        shipment.CarrierLabelUrl = apiResult.LabelUrl;
        shipment.CarrierMetaJson = apiResult.RawJson;
        shipment.TrackingNumber = apiResult.TrackingNumber ?? shipment.TrackingNumber;
        shipment.ShipmentStatus = ShipmentStatuses.ReadyForPickup;
        shipment.LastModified = now;
        shipment.LastModifiedBy = username;

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateCarrierShipmentResult(
            shipment.Id,
            carrierCode,
            shipment.CarrierOrderCode,
            shipment.TrackingNumber,
            shipment.ShipmentStatus);
    }
}
