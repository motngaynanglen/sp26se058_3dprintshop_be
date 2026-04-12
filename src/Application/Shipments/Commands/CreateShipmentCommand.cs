using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
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
            throw new BusinessException($"Chỉ có thể tạo vận đơn khi đơn hàng đang ở trạng thái '{OrderStatuses.Processing}'.",
                ResponseCodeConstants.VAL_BUSINESS_RESTRICTION);
        }
        // Quy tắc 1: Chỉ ship 1 lần. 
        // Kiểm tra xem đơn hàng đã có Shipment nào chưa bị Cancelled/Failed (đang xử lý hoặc đã xong) không.
        var hasActiveShipment = order.Shipments?
                    .Any(s => s.ShipmentStatus != ShipmentStatuses.Cancelled
                            && s.ShipmentStatus != ShipmentStatuses.Failed);

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

        Guid? finalShippingAddressId = null;
        if (request.ShippingAddressId.HasValue)
        {
            var validAddress = await _context.ShippingAddresses
                .AnyAsync(a => a.Id == request.ShippingAddressId && a.CustomerId == order.CustomerId, ct);
            if (!validAddress)
            {
                throw new BusinessException("Địa chỉ giao hàng không hợp lệ hoặc không thuộc về khách hàng của đơn hàng này.", ResponseCodeConstants.FORBIDDEN);
            }

            finalShippingAddressId = request.ShippingAddressId;
        }
        // Ưu tiên 2: Địa chỉ từ vận đơn cũ (nếu có shipment đã bị cancel/fail trước đó)
        else
        {
            finalShippingAddressId = order.Shipments?
                .OrderByDescending(s => s.Created)
                .FirstOrDefault()?.ShippingAddressId;
            
        }
        if (finalShippingAddressId == null || finalShippingAddressId == Guid.Empty)
        {
            throw new BusinessException("Không tìm thấy địa chỉ giao hàng hợp lệ. Vui lòng cung cấp ShippingAddressId.");
        }
        // 2. Khởi tạo Shipment (Lấy logic từ Checkout)
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            // Nếu request có địa chỉ mới thì dùng, không thì lấy từ đơn hàng cũ
            ShippingAddressId = finalShippingAddressId.Value,
            ShippingFee = 0, 
            ShipmentStatus = ShipmentStatuses.Preparing,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };

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
            throw new UpdateFailureException($"Lỗi tạo vận đơn: {ex.InnerException?.Message ?? ex.Message}");
        }

        return _mapper.Map<ShipmentDTO>(shipment);
    }
}
