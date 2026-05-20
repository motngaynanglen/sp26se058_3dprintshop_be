using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Shipments;
using sp26se058_3dprintshop_be.Application.Shipments.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]

public record CreateShipmentCommand : IRequest<object>
{
    public Guid OrderId { get; init; }
    public Guid? ShippingAddressId { get; init; }
}
public class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CreateShipmentCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<object> Handle(CreateShipmentCommand request, CancellationToken ct)
    {
        // 1. Load Order kèm theo Shipments cũ và OrderItems
        var order = await _context.Orders
            .Include(o => o.Shipments)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

        // --- KIỂM TRA ĐIỀU KIỆN TIÊN QUYẾT (Throw ngay) ---
        if (order == null) throw new DataNotFoundException(nameof(Order), request.OrderId);
        if (order.OrderStatus != OrderStatuses.Processing)
        {
            var processingLabel = OrderStatuses.All
                .FirstOrDefault(x => x.Value == OrderStatuses.Processing)?.Label
                ?? OrderStatuses.Processing;
            throw new BusinessException($"Chỉ có thể tạo vận đơn khi đơn hàng đang ở trạng thái {processingLabel}.",
                ResponseCodeConstants.VAL_BUSINESS_RESTRICTION);
        }
        // Quy tắc 1: Chỉ có một lượt giao đang xử lý tại một thời điểm.
        var hasActiveShipment = order.Shipments?
                    .Any(s => s.ShipmentStatus != ShipmentStatuses.Cancelled
                            && s.ShipmentStatus != ShipmentStatuses.Returned
                            && s.ShipmentStatus != ShipmentStatuses.LostOrDamaged);

        if (hasActiveShipment == true)
        {
            throw new BusinessException("Đơn hàng này đã có vận đơn đang tồn tại. Không thể tạo thêm.",
                ResponseCodeConstants.VAL_BUSINESS_RESTRICTION);
        }

        // Quy tắc 2: Chỉ tạo Shipment nếu đơn hàng có sản phẩm vật lý (không phải chỉ toàn Design Service)
        var hasPhysicalItem = order.OrderItems
            .Any(oi => oi.SourceType != SourceTypes.DesignService);

        if (!hasPhysicalItem)
        {
            throw new BusinessException("Đơn hàng này chỉ bao gồm các dịch vụ thiết kế, không cần tạo vận đơn vật lý.",
                ResponseCodeConstants.VAL_BUSINESS_RESTRICTION);
        }

        ShippingAddress? finalShippingAddress = null;
        if (request.ShippingAddressId.HasValue)
        {
            finalShippingAddress = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a => a.Id == request.ShippingAddressId && a.CustomerId == order.CustomerId, ct);
            if (finalShippingAddress == null)
            {
                throw new BusinessException("Địa chỉ giao hàng không hợp lệ hoặc không thuộc về khách hàng của đơn hàng này.", ResponseCodeConstants.FORBIDDEN);
            }
        }
        // Ưu tiên 2: Địa chỉ từ vận đơn cũ (nếu có shipment đã bị cancel/fail trước đó)
        else
        {
            var previousShippingAddressId = order.Shipments?
                .OrderByDescending(s => s.Created)
                .FirstOrDefault()?.ShippingAddressId;

            if (previousShippingAddressId.HasValue)
            {
                finalShippingAddress = await _context.ShippingAddresses
                    .FirstOrDefaultAsync(a => a.Id == previousShippingAddressId.Value && a.CustomerId == order.CustomerId, ct);
            }
        }
        if (finalShippingAddress == null)
        {
            throw new BusinessException("Không tìm thấy địa chỉ giao hàng hợp lệ. Vui lòng cung cấp mã địa chỉ giao hàng.");
        }
        // 2. Khởi tạo Shipment (Lấy logic từ Checkout)
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ShippingFee = 0, 
            ShipmentStatus = ShipmentStatuses.Preparing,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };
        ShipmentAddressSnapshot.Apply(shipment, finalShippingAddress);

        // 3. Cập nhật Order Status (Nếu cần)
        // Khi tạo vận đơn, đơn hàng nên được xác nhận là đang xử lý
        if (order.OrderStatus == OrderStatuses.Pending)
        {
            order.OrderStatus = OrderStatuses.Processing;
        }

        _context.Shipments.Add(shipment);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new CreateFailureException(nameof(Shipment),$"{ex.InnerException?.Message ?? ex.Message}");
        }

        return _mapper.Map<ShipmentDTO>(shipment);
    }
}
