using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;
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

    public ConfirmShipmentDeliveredCommandHandler(IApplicationDbContext context, IMapper mapper ,IUser user)
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

        if (shipment == null) throw new Exception("Không tìm thấy thông tin vận chuyển.");

        // Chỉ cho phép hoàn thành khi hàng đang đi (InTransit)
        if (shipment.ShipmentStatus != ShipmentStatuses.InTransit)
        {
            throw new Exception("Chỉ có thể xác nhận giao hàng cho các đơn đang được vận chuyển.");
        }

        // 1. Cập nhật Shipment
        shipment.ShipmentStatus = ShipmentStatuses.Delivered;
        shipment.DeliveredAt = CoreHelper.SystemTimeNow.UtcDateTime;
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;
        // 2. Cập nhật Order - có thể cập nhật Note: "Chờ khách hàng nghiệm thu"
        //shipment.Order.Note = "Hàng đã giao thành công. Chờ xác nhận từ khách hàng.";
        
        await _context.SaveChangesAsync(ct);
        return _mapper.Map<ShipmentDTO>(shipment);
    }
}
