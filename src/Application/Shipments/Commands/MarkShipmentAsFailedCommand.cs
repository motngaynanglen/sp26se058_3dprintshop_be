using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]

public record MarkShipmentAsFailedCommand : IRequest<object>
{
    public Guid Id { get; init; }
    public string Reason { get; init; } = null!; // Lý do thất bại
}
public class MarkShipmentAsFailedCommandHandler : IRequestHandler<MarkShipmentAsFailedCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public MarkShipmentAsFailedCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<object> Handle(MarkShipmentAsFailedCommand request, CancellationToken ct)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (shipment == null)
            throw new DataNotFoundException(nameof(Shipment), request.Id);

        // Chỉ có thể thất bại khi đang đi giao
        var terminalStatuses = new[] { ShipmentStatuses.Delivered, ShipmentStatuses.Cancelled, ShipmentStatuses.Returned };
        if (terminalStatuses.Contains(shipment.ShipmentStatus))
        {
            throw new BusinessException(
                $"Kiện hàng đã ở trạng thái kết thúc ({shipment.ShipmentStatus}), không thể báo giao thất bại.",
                ResponseCodeConstants.VAL_INVALID_STATE);
        }

        // 1. Cập nhật trạng thái Shipment
        shipment.ShipmentStatus = ShipmentStatuses.Failed;
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;


        await _context.SaveChangesAsync(ct);
        return true;
    }
}
