using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Queries;

/// <summary>[Staff/Manager] Lấy URL phiếu in (printA5) cho vận đơn GHN.</summary>
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record GetCarrierShipmentLabelQuery : IRequest<CarrierShipmentLabelResult>
{
    public Guid Id { get; init; }
}

public record CarrierShipmentLabelResult(string? LabelUrl, string? Carrier, string? CarrierOrderCode);

public class GetCarrierShipmentLabelQueryHandler
    : IRequestHandler<GetCarrierShipmentLabelQuery, CarrierShipmentLabelResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IShippingCarrierResolver _carriers;

    public GetCarrierShipmentLabelQueryHandler(
        IApplicationDbContext context,
        IShippingCarrierResolver carriers)
    {
        _context = context;
        _carriers = carriers;
    }

    public async Task<CarrierShipmentLabelResult> Handle(GetCarrierShipmentLabelQuery request, CancellationToken ct)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new DataNotFoundException(nameof(Shipment), request.Id);

        if (!ShippingCarriers.IsThirdParty(shipment.Carrier)
            || string.IsNullOrWhiteSpace(shipment.CarrierOrderCode))
            throw new BusinessException(
                "Vận đơn chưa được tạo trên đơn vị vận chuyển nên chưa có phiếu in.",
                ResponseCodeConstants.VAL_INVALID_STATE);

        // Đã có URL lưu sẵn → trả luôn.
        if (!string.IsNullOrWhiteSpace(shipment.CarrierLabelUrl))
            return new CarrierShipmentLabelResult(shipment.CarrierLabelUrl, shipment.Carrier, shipment.CarrierOrderCode);

        var service = _carriers.GetService(shipment.Carrier!)
            ?? throw new BusinessException(
                $"Không hỗ trợ đơn vị vận chuyển: {shipment.Carrier}.",
                ResponseCodeConstants.VAL_INVALID_STATE);

        var url = await service.GetLabelUrlAsync(shipment.CarrierOrderCode!, ct);
        if (string.IsNullOrWhiteSpace(url))
            throw new BusinessException(
                "Không lấy được phiếu in từ đơn vị vận chuyển. Vui lòng thử lại sau.",
                ResponseCodeConstants.EXTERNAL_ERROR);

        shipment.CarrierLabelUrl = url;
        shipment.LastModified = CoreHelper.SystemTimeNow;
        await _context.SaveChangesAsync(ct);

        return new CarrierShipmentLabelResult(url, shipment.Carrier, shipment.CarrierOrderCode);
    }
}
