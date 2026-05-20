using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record ApproveShipmentAddressChangeCommand : IRequest<ShipmentAddressChangeRequestDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public string? ResponseNote { get; init; }
}

public class ApproveShipmentAddressChangeCommandHandler : IRequestHandler<ApproveShipmentAddressChangeCommand, ShipmentAddressChangeRequestDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public ApproveShipmentAddressChangeCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<ShipmentAddressChangeRequestDTO> Handle(ApproveShipmentAddressChangeCommand request, CancellationToken ct)
    {
        var entity = await _context.ShipmentAddressChangeRequests
            .Include(x => x.Shipment)
            .Include(x => x.NewShippingAddress)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (entity == null)
            throw new DataNotFoundException(nameof(ShipmentAddressChangeRequest), request.Id);

        if (entity.Status != ShipmentAddressChangeRequestStatuses.Pending)
            throw new BusinessException("Yêu cầu đổi địa chỉ này đã được xử lý trước đó.", ResponseCodeConstants.VAL_INVALID_STATE);

        var shipment = entity.Shipment;
        var allowedStatuses = new[] { ShipmentStatuses.Preparing, ShipmentStatuses.ReadyForPickup };
        if (!allowedStatuses.Contains(shipment.ShipmentStatus))
        {
            throw new BusinessException("Không thể duyệt đổi địa chỉ vì vận đơn đã được bàn giao hoặc đã kết thúc.", ResponseCodeConstants.VAL_INVALID_STATE);
        }

        ShipmentAddressSnapshot.Apply(shipment, entity.NewShippingAddress);
        shipment.Note = AppendNote(shipment.Note, $"Duyệt đổi địa chỉ giao hàng. {request.ResponseNote}".Trim());
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;

        entity.Status = ShipmentAddressChangeRequestStatuses.Approved;
        entity.ResponseNote = request.ResponseNote;
        entity.ReviewedByAccountId = _user.Id.ToGuid();
        entity.ReviewedAt = CoreHelper.SystemTimeNow;
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(ct);
        return _mapper.Map<ShipmentAddressChangeRequestDTO>(entity);
    }

    private static string AppendNote(string? currentNote, string nextNote)
    {
        var timestamp = CoreHelper.SystemTimeNow.ToString("dd/MM/yyyy HH:mm");
        var line = $"[{timestamp}] {nextNote}";
        return string.IsNullOrWhiteSpace(currentNote) ? line : $"{currentNote}{Environment.NewLine}{line}";
    }
}
