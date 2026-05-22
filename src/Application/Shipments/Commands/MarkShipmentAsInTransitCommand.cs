using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]

public record MarkShipmentAsInTransitCommand : IRequest<object>
{
    [JsonIgnore]
    public Guid Id { get; init; }

    // Tích hợp thay vì dùng ID của bảng ShippingMethod
    [DefaultValue("ViettelPost")]
    public string CarrierName { get; init; } = null!; // Ví dụ: GHTK, GHN, Grab, ViettelPost
    [DefaultValue("SPX123456789")]
    public string TrackingNumber { get; init; } = null!;
    public DateTime ShippedAt { get; init; } = DateTime.UtcNow;
    public DateTime EstimatedDeliveryTime { get; set; }

}
public class MarkShipmentAsInTransitCommandHandler : IRequestHandler<MarkShipmentAsInTransitCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public MarkShipmentAsInTransitCommandHandler(IApplicationDbContext context,IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<object> Handle(MarkShipmentAsInTransitCommand request, CancellationToken ct)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (shipment == null)
            throw new DataNotFoundException(nameof(Shipment), request.Id);
        if (shipment.ShipmentStatus == ShipmentStatuses.InTransit)
        {
            throw new BusinessException("Kiện hàng này đã được xác nhận đang vận chuyển.", ResponseCodeConstants.VAL_INVALID_STATE);
        }
        // Kiểm tra logic: Chỉ cho phép đi giao khi đã sẵn sàng (ReadyForPickup)
        if (shipment.ShipmentStatus != ShipmentStatuses.ReadyForPickup)
        {
            throw new BusinessException(
                $"Kiện hàng đang ở trạng thái '{shipment.ShipmentStatus}', không thể bắt đầu giao hàng.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        // Cập nhật thông tin vận chuyển tích hợp
        shipment.ShipmentStatus = ShipmentStatuses.InTransit;
        shipment.CarrierName = request.CarrierName;
        shipment.TrackingNumber = request.TrackingNumber;
        shipment.EstimatedDeliveryTime = request.EstimatedDeliveryTime;

        shipment.ShippedAt = request.ShippedAt;
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;


        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException($"Lỗi khi cập nhật trạng thái đang giao hàng: {ex.Message}");
        }
        return _mapper.Map<ShipmentDTO>(shipment);
    }
}
