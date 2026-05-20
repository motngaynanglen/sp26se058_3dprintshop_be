using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record RejectShipmentAddressChangeCommand : IRequest<ShipmentAddressChangeRequestDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public string ResponseNote { get; init; } = null!;
}

public class RejectShipmentAddressChangeCommandValidator : AbstractValidator<RejectShipmentAddressChangeCommand>
{
    public RejectShipmentAddressChangeCommandValidator()
    {
        RuleFor(x => x.ResponseNote)
            .NotEmpty().WithMessage("Vui lòng nhập lý do từ chối yêu cầu đổi địa chỉ.")
            .MaximumLength(500).WithMessage("Lý do từ chối không được vượt quá 500 ký tự.");
    }
}

public class RejectShipmentAddressChangeCommandHandler : IRequestHandler<RejectShipmentAddressChangeCommand, ShipmentAddressChangeRequestDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public RejectShipmentAddressChangeCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<ShipmentAddressChangeRequestDTO> Handle(RejectShipmentAddressChangeCommand request, CancellationToken ct)
    {
        var entity = await _context.ShipmentAddressChangeRequests
            .Include(x => x.NewShippingAddress)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (entity == null)
            throw new DataNotFoundException(nameof(ShipmentAddressChangeRequest), request.Id);

        if (entity.Status != ShipmentAddressChangeRequestStatuses.Pending)
            throw new BusinessException("Yêu cầu đổi địa chỉ này đã được xử lý trước đó.", ResponseCodeConstants.VAL_INVALID_STATE);

        entity.Status = ShipmentAddressChangeRequestStatuses.Rejected;
        entity.ResponseNote = request.ResponseNote;
        entity.ReviewedByAccountId = _user.Id.ToGuid();
        entity.ReviewedAt = CoreHelper.SystemTimeNow;
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(ct);
        return _mapper.Map<ShipmentAddressChangeRequestDTO>(entity);
    }
}
