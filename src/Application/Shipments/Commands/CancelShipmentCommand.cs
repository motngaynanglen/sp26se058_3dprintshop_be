using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record CancelShipmentCommand : IRequest<ShipmentDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public string Reason { get; init; } = null!;
}

public class CancelShipmentCommandValidator : AbstractValidator<CancelShipmentCommand>
{
    public CancelShipmentCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Vui lòng nhập lý do hủy vận đơn.")
            .MaximumLength(500).WithMessage("Lý do hủy vận đơn không được vượt quá 500 ký tự.");
    }
}

public class CancelShipmentCommandHandler : IRequestHandler<CancelShipmentCommand, ShipmentDTO>
{
    private static readonly string[] CancellableStatuses =
    [
        ShipmentStatuses.Preparing,
        ShipmentStatuses.ReadyForPickup
    ];

    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    private readonly IShippingCarrierResolver _carriers;

    public CancelShipmentCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user, IShippingCarrierResolver carriers)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
        _carriers = carriers;
    }

    public async Task<ShipmentDTO> Handle(CancelShipmentCommand request, CancellationToken ct)
    {
        var shipment = await _context.Shipments
            .Include(s => s.AddressChangeRequests)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (shipment == null)
            throw new DataNotFoundException(nameof(Shipment), request.Id);

        if (shipment.ShipmentStatus == ShipmentStatuses.Cancelled)
            throw new BusinessException("Vận đơn này đã được hủy trước đó.", ResponseCodeConstants.VAL_INVALID_STATE);

        if (!CancellableStatuses.Contains(shipment.ShipmentStatus))
            throw new BusinessException("Chỉ có thể hủy vận đơn khi chưa bàn giao cho đơn vị vận chuyển.", ResponseCodeConstants.VAL_INVALID_STATE);

        // Đơn GHN đã tạo vận đơn thật → phải hủy bên GHN trước, tránh shipper vẫn đến lấy.
        if (ShippingCarriers.IsThirdParty(shipment.Carrier)
            && !string.IsNullOrWhiteSpace(shipment.CarrierOrderCode))
        {
            var service = _carriers.GetService(shipment.Carrier!);
            var cancelledAtCarrier = service != null
                && await service.CancelShipmentAsync(shipment.CarrierOrderCode!, ct);
            if (!cancelledAtCarrier)
                throw new BusinessException(
                    "Không hủy được vận đơn bên đơn vị vận chuyển (có thể shipper đã lấy hàng). Kiểm tra lại trên hệ thống GHN.",
                    ResponseCodeConstants.VAL_INVALID_STATE);
        }

        shipment.ShipmentStatus = ShipmentStatuses.Cancelled;
        shipment.Note = AppendNote(shipment.Note, $"Hủy vận đơn: {request.Reason}");
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;

        foreach (var changeRequest in shipment.AddressChangeRequests
            .Where(x => x.Status == ShipmentAddressChangeRequestStatuses.Pending))
        {
            changeRequest.Status = ShipmentAddressChangeRequestStatuses.Rejected;
            changeRequest.ResponseNote = "Vận đơn đã bị hủy.";
            changeRequest.ReviewedByAccountId = _user.Id.ToGuid();
            changeRequest.ReviewedAt = CoreHelper.SystemTimeNow;
            changeRequest.LastModified = CoreHelper.SystemTimeNow;
            changeRequest.LastModifiedBy = _user.Username;
        }

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
