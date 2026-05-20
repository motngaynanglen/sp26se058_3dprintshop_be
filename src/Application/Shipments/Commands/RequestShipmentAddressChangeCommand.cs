using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;

[Authorize(Roles = Roles.CUSTOMER)]
public record RequestShipmentAddressChangeCommand : IRequest<ShipmentAddressChangeRequestDTO>
{
    [JsonIgnore]
    public Guid ShipmentId { get; init; }
    public Guid NewShippingAddressId { get; init; }
    public string Reason { get; init; } = null!;
}

public class RequestShipmentAddressChangeCommandValidator : AbstractValidator<RequestShipmentAddressChangeCommand>
{
    public RequestShipmentAddressChangeCommandValidator()
    {
        RuleFor(x => x.NewShippingAddressId)
            .NotEmpty().WithMessage("Địa chỉ nhận hàng mới không được để trống.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Vui lòng nhập lý do đổi địa chỉ.")
            .MaximumLength(500).WithMessage("Lý do đổi địa chỉ không được vượt quá 500 ký tự.");
    }
}

public class RequestShipmentAddressChangeCommandHandler : IRequestHandler<RequestShipmentAddressChangeCommand, ShipmentAddressChangeRequestDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public RequestShipmentAddressChangeCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<ShipmentAddressChangeRequestDTO> Handle(RequestShipmentAddressChangeCommand request, CancellationToken ct)
    {
        var userId = _user.Id.ToGuid();
        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.AccountId == userId, ct);
        if (customer == null)
            throw new ForbiddenAccessException("Chỉ tài khoản khách hàng mới có thể yêu cầu đổi địa chỉ giao hàng.");

        var shipment = await _context.Shipments
            .Include(s => s.Order)
            .Include(s => s.AddressChangeRequests)
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, ct);

        if (shipment == null)
            throw new DataNotFoundException(nameof(Shipment), request.ShipmentId);

        if (shipment.Order.CustomerId != customer.Id)
            throw new ForbiddenAccessException("Bạn không có quyền yêu cầu đổi địa chỉ cho vận đơn này.");

        var allowedStatuses = new[] { ShipmentStatuses.Preparing, ShipmentStatuses.ReadyForPickup };
        if (!allowedStatuses.Contains(shipment.ShipmentStatus))
        {
            throw new BusinessException("Chỉ có thể gửi yêu cầu đổi địa chỉ khi vận đơn chưa được bàn giao cho đơn vị vận chuyển.", ResponseCodeConstants.VAL_INVALID_STATE);
        }

        var newAddress = await _context.ShippingAddresses
            .FirstOrDefaultAsync(x => x.Id == request.NewShippingAddressId && x.CustomerId == customer.Id, ct);
        if (newAddress == null)
            throw new BusinessException("Địa chỉ giao hàng mới không tồn tại hoặc không thuộc quyền sở hữu của bạn.", ResponseCodeConstants.FORBIDDEN);

        var hasPendingRequest = shipment.AddressChangeRequests
            .Any(x => x.Status == ShipmentAddressChangeRequestStatuses.Pending);
        if (hasPendingRequest)
            throw new BusinessException("Vận đơn này đã có yêu cầu đổi địa chỉ đang chờ xử lý.", ResponseCodeConstants.VAL_INVALID_STATE);

        var entity = new ShipmentAddressChangeRequest
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            RequestedByCustomerId = customer.Id,
            NewShippingAddressId = newAddress.Id,
            Status = ShipmentAddressChangeRequestStatuses.Pending,
            Reason = request.Reason,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username
        };

        _context.ShipmentAddressChangeRequests.Add(entity);
        await _context.SaveChangesAsync(ct);

        entity.NewShippingAddress = newAddress;
        return _mapper.Map<ShipmentAddressChangeRequestDTO>(entity);
    }
}
