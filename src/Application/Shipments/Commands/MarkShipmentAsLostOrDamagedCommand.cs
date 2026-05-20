using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record MarkShipmentAsLostOrDamagedCommand : IRequest<ShipmentDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public string Reason { get; init; } = null!;
}

public class MarkShipmentAsLostOrDamagedCommandValidator : AbstractValidator<MarkShipmentAsLostOrDamagedCommand>
{
    public MarkShipmentAsLostOrDamagedCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Vui lòng nhập lý do thất lạc hoặc hư hỏng.")
            .MaximumLength(500).WithMessage("Lý do không được vượt quá 500 ký tự.");
    }
}

public class MarkShipmentAsLostOrDamagedCommandHandler : IRequestHandler<MarkShipmentAsLostOrDamagedCommand, ShipmentDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public MarkShipmentAsLostOrDamagedCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<ShipmentDTO> Handle(MarkShipmentAsLostOrDamagedCommand request, CancellationToken ct)
    {
        var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (shipment == null)
            throw new DataNotFoundException(nameof(Shipment), request.Id);

        if (shipment.ShipmentStatus != ShipmentStatuses.Returning)
        {
            throw new BusinessException("Chỉ có thể báo thất lạc hoặc hư hỏng khi kiện hàng đang chuyển hoàn.", ResponseCodeConstants.VAL_INVALID_STATE);
        }

        shipment.ShipmentStatus = ShipmentStatuses.LostOrDamaged;
        shipment.Note = AppendNote(shipment.Note, $"Thất lạc hoặc hư hỏng: {request.Reason}");
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
