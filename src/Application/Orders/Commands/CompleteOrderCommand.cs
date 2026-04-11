using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;
[Authorize(Roles = Roles.MANAGER + "," + Roles.STAFF + "," + Roles.CUSTOMER)]
public record CompleteOrderCommand : IRequest<object>
{
    public Guid Id { get; set; }
}
public class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CompleteOrderCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<object> Handle(CompleteOrderCommand request, CancellationToken ct)
    {
        var order = await _context.Orders
            .Include(o => o.Invoice)
            .Include(o => o.Shipments)
            .Include(o => o.OrderItems)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct);

        if (order == null) throw new Exception("Không tìm thấy đơn hàng.");

        // --- KIỂM TRA 1: HÀNG VẬT LÝ (SHIPMENTS) ---
        // Nếu đơn hàng có yêu cầu giao vận
        var shipment = order.Shipments?.FirstOrDefault();
        if (shipment != null)
        {
            // Nếu đã xác định gửi 1 lần, thì shipment này BẮT BUỘC phải Delivered
            if (shipment.ShipmentStatus != ShipmentStatuses.Delivered)
            {
                var statusMsg = shipment.ShipmentStatus == ShipmentStatuses.Failed
                    ? "gặp sự cố (Failed)"
                    : "đang trong quá trình vận chuyển";
                throw new Exception($"Không thể hoàn thành đơn hàng vì kiện hàng {statusMsg}.");
            }
        }

        // --- KIỂM TRA 2: HÀNG THIẾT KẾ (DESIGN SERVICES) ---
        var hasUnfinishedDesign = order.OrderItems
                .Any(oi => oi.SourceType == SourceTypes.DesignService
                    && oi.FulfillmentStatus != OrderItemStatuses.Finished);
        if (hasUnfinishedDesign)
        {
            throw new Exception("Vẫn còn dịch vụ thiết kế chưa hoàn tất bàn giao file cho khách.");
        }

        bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;
        bool isOwner = order.Customer.AccountId == _user.Id.ToGuid();

        if (isStaffOrManager && !isOwner)
        {
            if (shipment != null && shipment.DeliveredAt.HasValue)
            {
                var daysSinceDelivered = (DateTime.UtcNow - shipment.DeliveredAt.Value).TotalDays;
                if (daysSinceDelivered < 3)
                {
                    var remainingDays = Math.Ceiling(3 - daysSinceDelivered);
                    throw new Exception($"Chỉ Manager/Staff mới có thể chốt đơn sau 3 ngày kể từ khi giao hàng. Vui lòng đợi thêm {remainingDays} ngày.");
                }
            }
            // Lưu ý: Nếu đơn chỉ có Design Service, Bách có thể áp dụng logic tương tự với LastModified của DesignItem
        }

        // 1. Chuyển trạng thái Order sang COMPLETED
        order.OrderStatus = OrderStatuses.Completed;
        order.LastModified = CoreHelper.SystemTimeNow;
        order.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(ct);
        return _mapper.Map<OrderDTO>(order);
    }
}
