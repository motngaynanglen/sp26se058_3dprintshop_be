using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record ConfirmShipmentReturnedCommand : IRequest<ShipmentDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public string? Note { get; init; }
}

public class ConfirmShipmentReturnedCommandHandler : IRequestHandler<ConfirmShipmentReturnedCommand, ShipmentDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public ConfirmShipmentReturnedCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<ShipmentDTO> Handle(ConfirmShipmentReturnedCommand request, CancellationToken ct)
    {
        var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (shipment == null)
            throw new DataNotFoundException(nameof(Shipment), request.Id);

        if (shipment.ShipmentStatus != ShipmentStatuses.Returning)
        {
            throw new BusinessException("Chỉ có thể xác nhận đã hoàn hàng khi kiện hàng đang trong quá trình chuyển hoàn.", ResponseCodeConstants.VAL_INVALID_STATE);
        }

        shipment.ShipmentStatus = ShipmentStatuses.Returned;
        shipment.Note = AppendNote(shipment.Note, $"Đã nhận hàng hoàn. {request.Note}".Trim());
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(ct);
        return _mapper.Map<ShipmentDTO>(shipment);
    }

    private static string AppendNote(string? currentNote, string nextNote)
    {
        var timestamp = CoreHelper.SystemTimeNow.ToString("dd/MM/yyyy HH:mm");
        var line = $"[{timestamp}] {nextNote}";
        return string.IsNullOrWhiteSpace(currentNote) ? line : $"{currentNote}{Environment.NewLine}{line}";
    }
}
