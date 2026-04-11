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
public record MarkShipmentAsInTransitCommand : IRequest<object>
{
    [JsonIgnore]
    public Guid Id { get; init; }

    // Tích hợp thay vì dùng ID của bảng ShippingMethod
    public string CarrierName { get; init; } = null!; // Ví dụ: GHTK, GHN, Grab, ViettelPost
    public string TrackingNumber { get; init; } = null!;
    public DateTime ShippedAt { get; init; } = DateTime.UtcNow;
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

        if (shipment == null) throw new Exception("Không tìm thấy thông tin vận chuyển.");

        // Kiểm tra logic: Chỉ cho phép đi giao khi đã sẵn sàng (ReadyForPickup)
        if (shipment.ShipmentStatus != ShipmentStatuses.ReadyForPickup)
        {
            throw new Exception("Hàng hóa chưa sẵn sàng hoặc chưa đóng gói xong để bắt đầu giao.");
        }

        // Cập nhật thông tin vận chuyển tích hợp
        shipment.ShipmentStatus = ShipmentStatuses.InTransit;
        shipment.TrackingNumber = request.TrackingNumber;


        shipment.ShippedAt = request.ShippedAt;
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;

        

        await _context.SaveChangesAsync(ct);
        return _mapper.Map<ShipmentDTO>(shipment);
    }
}
