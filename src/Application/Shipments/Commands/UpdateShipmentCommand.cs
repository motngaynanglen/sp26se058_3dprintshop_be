using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;
public record UpdateShipmentCommand : IRequest<Guid>
{
    [JsonIgnore]
    public Guid Id { get; set; }
    [DefaultValue("SPX123456789")]
    public string? TrackingNumber { get; set; }
    [DefaultValue("SHIPPED")] // PENDING, SHIPPED, DELIVERED, FAILED
    public string? ShipmentStatus { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
}
public class UpdateShipmentCommandHandler : IRequestHandler<UpdateShipmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public UpdateShipmentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(UpdateShipmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Shipments
            .Include(s => s.Order)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null) throw new Exception("Không tìm thấy thông tin vận đơn");

        // Logic Patch: Chỉ cập nhật nếu không null
        entity.TrackingNumber = request.TrackingNumber ?? entity.TrackingNumber;
        entity.EstimatedDeliveryTime = request.EstimatedDeliveryTime ?? entity.EstimatedDeliveryTime;

        if (!string.IsNullOrEmpty(request.ShipmentStatus))
        {
            entity.ShipmentStatus = request.ShipmentStatus;

            // Logic nghiệp vụ đi kèm:
            if (request.ShipmentStatus == "SHIPPED")
                entity.ShippedAt = DateTime.UtcNow;

            if (request.ShipmentStatus == "DELIVERED")
            {
                entity.DeliveredAt = DateTime.UtcNow;
                entity.Order.OrderStatus = "COMPLETED"; // Tự động hoàn thành đơn hàng
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
