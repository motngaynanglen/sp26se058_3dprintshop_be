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
public record MarkShipmentAsReadyCommand : IRequest<object>
{
    [JsonIgnore]
    public Guid Id { get; set; }
}
public class MarkShipmentAsReadyCommandHandler : IRequestHandler<MarkShipmentAsReadyCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public MarkShipmentAsReadyCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<object> Handle(MarkShipmentAsReadyCommand request, CancellationToken ct)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Order)
                .ThenInclude(o => o.OrderItems)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (shipment == null)
        {
            throw new Exception("Không tìm thấy thông tin vận chuyển.");
        }

        bool isAnyItemNotFinished = shipment.Order.OrderItems
            .Any(item => item.FulfillmentStatus != OrderItemStatuses.Finished);

        if (isAnyItemNotFinished)
        {
            throw new Exception("Không thể chuyển trạng thái! Vẫn còn món hàng chưa hoàn thành sản xuất hoặc đóng gói.");
        }

        shipment.ShipmentStatus = ShipmentStatuses.ReadyForPickup;

        shipment.Created = CoreHelper.SystemTimeNow;
        shipment.CreatedBy = _user.Username;
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(ct);
        return _mapper.Map<ShipmentDTO>(shipment);
    }
}
