using System;
using System.Collections.Generic;
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
public record ConfirmShipmentDeliveredCommand : IRequest<object>
{
    [JsonIgnore]
    public Guid Id { get; set; }
}
public class ConfirmShipmentDeliveredCommandHandler : IRequestHandler<ConfirmShipmentDeliveredCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public ConfirmShipmentDeliveredCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<object> Handle(ConfirmShipmentDeliveredCommand request, CancellationToken ct)
    {
        // Load Shipment kèm theo Order để kết thúc vòng đời
        var shipment = await _context.Shipments
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (shipment == null)
            throw new DataNotFoundException(nameof(Shipment), request.Id);
        if (shipment.ShipmentStatus == ShipmentStatuses.Delivered)
        {
            throw new BusinessException("Kiện hàng này đã được xác nhận giao thành công trước đó.", ResponseCodeConstants.VAL_INVALID_STATE);
        }
        // Chỉ cho phép hoàn thành khi hàng đang đi (InTransit)
        if (shipment.ShipmentStatus != ShipmentStatuses.InTransit)
        {
            throw new BusinessException(
                 $"Không thể xác nhận giao hàng khi kiện hàng đang ở trạng thái '{shipment.ShipmentStatus}'.",
                 ResponseCodeConstants.VAL_INVALID_STATE);
        }

        // 1. Cập nhật Shipment
        shipment.ShipmentStatus = ShipmentStatuses.Delivered;
        shipment.DeliveredAt = CoreHelper.SystemTimeNow.UtcDateTime;
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;
        // 2. Cập nhật Order - có thể cập nhật Note: "Chờ khách hàng nghiệm thu"
        if (shipment.Order != null)
        {
            // Nếu đơn hàng đang ở trạng thái Pending/Processing, cập nhật nó theo Shipment

            shipment.Order.DeliveredAt = CoreHelper.SystemTimeNow;
            shipment.Order.LastModified = CoreHelper.SystemTimeNow;
            shipment.Order.LastModifiedBy = _user.Username;

            // Bạn có thể lưu vết vào Note của Order để khách hàng thấy trên UI
            // shipment.Order.Note += $"\nKiện hàng đã giao tới nơi vào lúc {CoreHelper.SystemTimeNow:dd/MM/yyyy HH:mm}.";
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException($"Lỗi khi xác nhận giao hàng: {ex.Message}");
        }
        return _mapper.Map<ShipmentDTO>(shipment);
    }
}
