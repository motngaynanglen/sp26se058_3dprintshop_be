using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

public record ProcessCarrierWebhookCommand : IRequest<bool>
{
    public required string Carrier { get; init; }

    public IReadOnlyDictionary<string, string> Payload { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public class ProcessCarrierWebhookCommandHandler : IRequestHandler<ProcessCarrierWebhookCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IShippingCarrierResolver _carriers;

    public ProcessCarrierWebhookCommandHandler(
        IApplicationDbContext context,
        IShippingCarrierResolver carriers)
    {
        _context = context;
        _carriers = carriers;
    }

    public async Task<bool> Handle(ProcessCarrierWebhookCommand request, CancellationToken cancellationToken)
    {
        var carrier = request.Carrier.Trim().ToUpperInvariant();
        var service = _carriers.GetService(carrier);
        if (service == null)
            return false;

        var orderRef = ResolveOrderReference(carrier, request.Payload);
        var statusRaw = ResolveStatus(carrier, request.Payload);
        if (string.IsNullOrWhiteSpace(orderRef))
            return false;

        var shipment = await _context.Shipments
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s =>
                    s.CarrierOrderCode == orderRef
                    || s.Order.Code == orderRef
                    || s.TrackingNumber == orderRef,
                cancellationToken);

        if (shipment == null)
            return false;

        var now = CoreHelper.SystemTimeNow;
        shipment.Carrier = carrier;
        shipment.CarrierStatus = statusRaw;
        shipment.CarrierMetaJson = JsonSerializer.Serialize(request.Payload);

        if (!string.IsNullOrWhiteSpace(statusRaw)
            && service.TryMapCarrierStatus(statusRaw, out var mapped))
        {
            shipment.ShipmentStatus = mapped;
            if (mapped == ShipmentStatuses.InTransit && shipment.ShippedAt == null)
                shipment.ShippedAt = now.UtcDateTime;
            if (mapped == ShipmentStatuses.Delivered)
            {
                shipment.DeliveredAt = now.UtcDateTime;
                shipment.Order.DeliveredAt = now;
                if (shipment.Order.OrderStatus == OrderStatuses.Finished
                    || shipment.Order.OrderStatus == OrderStatuses.Processing)
                    shipment.Order.OrderStatus = OrderStatuses.Completed;
            }
        }

        shipment.LastModified = now;
        shipment.LastModifiedBy = "carrier-webhook";
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? ResolveOrderReference(string carrier, IReadOnlyDictionary<string, string> p)
    {
        return First(p, "OrderCode", "order_code", "ClientOrderCode", "client_order_code");
    }

    private static string? ResolveStatus(string carrier, IReadOnlyDictionary<string, string> p)
    {
        return First(p, "Status", "status");
    }

    private static string? First(IReadOnlyDictionary<string, string> p, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (p.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
    }
}
