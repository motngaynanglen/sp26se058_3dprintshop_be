using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record MarkShipmentAsReturningCommand : IRequest<ShipmentDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public string? Reason { get; init; }
}

public class MarkShipmentAsReturningCommandHandler : IRequestHandler<MarkShipmentAsReturningCommand, ShipmentDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public MarkShipmentAsReturningCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<ShipmentDTO> Handle(MarkShipmentAsReturningCommand request, CancellationToken ct)
    {
        var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (shipment == null)
            throw new DataNotFoundException(nameof(Shipment), request.Id);

        var allowedStatuses = new[] { ShipmentStatuses.InTransit, ShipmentStatuses.Failed };
        if (!allowedStatuses.Contains(shipment.ShipmentStatus))
        {
            throw new BusinessException("Chỉ có thể chuyển hoàn khi kiện hàng đang giao hoặc đã giao thất bại.", ResponseCodeConstants.VAL_INVALID_STATE);
        }

        shipment.ShipmentStatus = ShipmentStatuses.Returning;
        shipment.Note = AppendNote(shipment.Note, $"Chuyển hoàn hàng. {request.Reason}".Trim());
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
