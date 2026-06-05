using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

/// <summary>[Staff/Manager] Pull trạng thái từ đơn vị vận chuyển (GHN) và cập nhật vận đơn.</summary>
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record SyncCarrierShipmentCommand : IRequest<ShipmentDTO>
{
    public Guid Id { get; init; }
}

public class SyncCarrierShipmentCommandHandler : IRequestHandler<SyncCarrierShipmentCommand, ShipmentDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IShippingCarrierResolver _carriers;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public SyncCarrierShipmentCommandHandler(
        IApplicationDbContext context,
        IShippingCarrierResolver carriers,
        IUser user,
        IMapper mapper)
    {
        _context = context;
        _carriers = carriers;
        _user = user;
        _mapper = mapper;
    }

    public async Task<ShipmentDTO> Handle(SyncCarrierShipmentCommand request, CancellationToken ct)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new DataNotFoundException(nameof(Shipment), request.Id);

        if (!ShippingCarriers.IsThirdParty(shipment.Carrier)
            || string.IsNullOrWhiteSpace(shipment.CarrierOrderCode))
            throw new BusinessException(
                "Vận đơn này không dùng đơn vị vận chuyển có hỗ trợ đồng bộ trạng thái.",
                ResponseCodeConstants.VAL_INVALID_STATE);

        var service = _carriers.GetService(shipment.Carrier!)
            ?? throw new BusinessException(
                $"Không hỗ trợ đơn vị vận chuyển: {shipment.Carrier}.",
                ResponseCodeConstants.VAL_INVALID_STATE);

        var statusRaw = await service.GetCarrierStatusAsync(shipment.CarrierOrderCode!, ct);
        if (string.IsNullOrWhiteSpace(statusRaw))
            throw new BusinessException(
                "Không lấy được trạng thái từ đơn vị vận chuyển. Vui lòng thử lại sau.",
                ResponseCodeConstants.EXTERNAL_ERROR);

        var now = CoreHelper.SystemTimeNow;
        shipment.CarrierStatus = statusRaw;

        if (service.TryMapCarrierStatus(statusRaw, out var mapped))
        {
            shipment.ShipmentStatus = mapped;
            if (mapped == ShipmentStatuses.InTransit && shipment.ShippedAt == null)
                shipment.ShippedAt = now.UtcDateTime;
            if (mapped == ShipmentStatuses.Delivered)
            {
                shipment.DeliveredAt = now.UtcDateTime;
                if (shipment.Order != null)
                {
                    shipment.Order.DeliveredAt = now;
                    if (shipment.Order.OrderStatus == OrderStatuses.Finished
                        || shipment.Order.OrderStatus == OrderStatuses.Processing)
                        shipment.Order.OrderStatus = OrderStatuses.Completed;
                }
            }
        }

        shipment.LastModified = now;
        shipment.LastModifiedBy = _user.Username ?? "staff";
        await _context.SaveChangesAsync(ct);

        return _mapper.Map<ShipmentDTO>(shipment);
    }
}
