using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
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
            throw new DataNotFoundException(nameof(Shipment), request.Id);
        if (shipment.ShipmentStatus == ShipmentStatuses.ReadyForPickup)
        {
            throw new BusinessException("Kiện hàng đã được xác nhận sẵn sàng từ trước.", ResponseCodeConstants.VAL_INVALID_STATE);
        }

        bool isAnyItemNotFinished = shipment.Order.OrderItems
            .Any(item => item.FulfillmentStatus != OrderItemStatuses.Finished);

        if (isAnyItemNotFinished)
        {
            throw new BusinessException(
                $"Vẫn còn sản phẩm trong đơn hàng {nameof(shipment.Order.OrderItems)} chưa hoàn thành sản xuất hoặc đóng gói. Hãy kiểm tra lại xưởng in.",
                ResponseCodeConstants.VAL_INVALID_STATE
                );
        }
        if (shipment.ShipmentStatus != ShipmentStatuses.Preparing)
        {
            throw new BusinessException(
                $"Không thể xác nhận sẵn sàng khi kiện hàng đang ở trạng thái '{shipment.ShipmentStatus}'.",
                ResponseCodeConstants.VAL_INVALID_STATE
                );
        }

        shipment.ShipmentStatus = ShipmentStatuses.ReadyForPickup;
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException($"Lỗi khi xác nhận kiện hàng sẵn sàng: {ex.Message}");
        }
        return _mapper.Map<ShipmentDTO>(shipment);
    }
}
