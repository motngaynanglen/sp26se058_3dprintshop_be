using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Shipping;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

/// <summary>[Staff/Manager] Tao van don GHN cho don hang da san sang giao.</summary>
public record CreateCarrierShipmentCommand : IRequest<CreateCarrierShipmentResult>
{
    public Guid OrderId { get; init; }

    /// <summary>GHN — <see cref="ShippingCarriers"/>.</summary>
    public required string Carrier { get; init; }

    public int? WeightGrams { get; init; }

    /// <summary>Bo sung ma quan GHN khi dia chi don thieu.</summary>
    public int? GhnDistrictId { get; init; }

    /// <summary>Bo sung ma phuong GHN khi dia chi don thieu.</summary>
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
            .WithMessage("Carrier phai la GHN hoac GHTK.");
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
            throw new UnauthorizedAccessException("Can dang nhap.");

        var role = _user.Role ?? Roles.GUEST;
        if (role != Roles.STAFF && role != Roles.MANAGER && role != Roles.ADMIN)
            throw new UnauthorizedAccessException("Chi nhan vien/quan ly tao duoc van don GHN.");

        var carrierCode = request.Carrier.Trim().ToUpperInvariant();
        var service = _carriers.GetService(carrierCode)
            ?? throw new InvalidOperationException($"Khong ho tro don vi van chuyen: {request.Carrier}");

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Invoice)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Khong tim thay don hang.");

        if (order.OrderStatus == OrderStatuses.Cancelled)
            throw new InvalidOperationException("Don hang da huy.");

        if (order.OrderStatus != OrderStatuses.Processing
            && order.OrderStatus != OrderStatuses.Finished)
            throw new InvalidOperationException(
                "Chi tao van don khi don o trang thai PROCESSING hoac FINISHED.");

        var shipment = await _context.Shipments
            .Include(s => s.ShippingAddress)
            .FirstOrDefaultAsync(s => s.OrderId == order.Id, cancellationToken)
            ?? throw new InvalidOperationException("Don hang chua co shipment.");

        if (!string.IsNullOrWhiteSpace(shipment.CarrierOrderCode))
            throw new InvalidOperationException(
                $"Da co van don {shipment.Carrier} — ma {shipment.CarrierOrderCode}.");

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
                $"Khong map duoc ma GHN tu dia chi «{addr.Ward}, {addr.District}, {addr.City}». "
                + "Kiem tra ten Phuong/Quan/Tinh khop danh muc GHN hoac chon lai tren form.");
        }

        var weight = request.WeightGrams ?? 500;
        var isPaid = order.Invoice?.PaymentStatus == InvoiceStatuses.Paid;

        var items = order.OrderItems.Select(oi => new CarrierShipmentLineItem(
            oi.ItemName ?? "San pham",
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
            throw new InvalidOperationException(apiResult.ErrorMessage ?? "Tao van don that bai.");

        var now = CoreHelper.SystemTimeNow;
        var username = _user.Username ?? "staff";

        // Lay luon URL phieu in (printA5) cho vac don vua tao.
        var labelUrl = apiResult.LabelUrl;
        if (string.IsNullOrWhiteSpace(labelUrl) && !string.IsNullOrWhiteSpace(apiResult.CarrierOrderCode))
            labelUrl = await service.GetLabelUrlAsync(apiResult.CarrierOrderCode, cancellationToken);

        shipment.Carrier = carrierCode;
        shipment.CarrierOrderCode = apiResult.CarrierOrderCode;
        shipment.CarrierStatus = apiResult.CarrierStatus;
        shipment.CarrierLabelUrl = labelUrl;
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
