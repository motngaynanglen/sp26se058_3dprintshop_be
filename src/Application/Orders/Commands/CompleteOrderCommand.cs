using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
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
        var failures = new List<ValidationFailure>();

        var order = await _context.Orders
            .Include(o => o.Invoice)
            .Include(o => o.Shipments)
            .Include(o => o.OrderItems)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct);

        if (order == null) throw new DataNotFoundException(nameof(Order), request.Id);
        if (order.OrderStatus == OrderStatuses.Completed)
        {
            throw new BusinessException("Đơn hàng này đã được hoàn thành trước đó.", ResponseCodeConstants.VAL_INVALID_STATE);
        }

        var userId = _user.Id.ToGuid();
        bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;
        bool isOwner = order.Customer?.AccountId == userId;
        if (!isOwner && !isStaffOrManager)
        {
            throw new ForbiddenAccessException("Bạn không có quyền hoàn tất đơn hàng này.");
        }

        // --- KIỂM TRA 1: HÀNG VẬT LÝ (SHIPMENTS) ---
        // Nếu đơn hàng có yêu cầu giao vận

        if (order.Shipments != null && order.Shipments.Any())
        {
            if (isOwner)
            {
                // KHÁCH HÀNG: Được phép chốt nếu đang giao (InTransit) hoặc đã giao (Delivered)
                var validStatusesForOwner = new[] { ShipmentStatuses.InTransit, ShipmentStatuses.Delivered };
                bool anyShipmentsValid = order.Shipments.Any(s => validStatusesForOwner.Contains(s.ShipmentStatus));
                if (!anyShipmentsValid)
                {
                    failures.Add(new ValidationFailure(nameof(order.OrderStatus),
                        "Vẫn còn kiện hàng chưa được gửi đi. Bạn chỉ có thể hoàn tất khi toàn bộ kiện hàng đang giao hoặc đã giao."));
                }
            }
            else if (isStaffOrManager)
            {
                // STAFF/MANAGER: Bắt buộc phải là Delivered thì mới được chốt hộ
                bool anyDelivered = order.Shipments.All(s => s.ShipmentStatus == ShipmentStatuses.Delivered);
                if (!anyDelivered)
                {
                    failures.Add(new ValidationFailure(nameof(ShipmentStatuses.Delivered),
                        "Nhân viên chỉ có thể chốt đơn khi toàn bộ kiện hàng đã giao thành công."));
                }

                // Quy tắc 3 ngày đối với Staff chốt hộ
                var latestDelivery = order.Shipments
                        .Where(s => s.DeliveredAt.HasValue && s.ShipmentStatus == ShipmentStatuses.Delivered)
                        .OrderByDescending(s => s.DeliveredAt)
                        .FirstOrDefault();

                if (latestDelivery != null)
                {
                    var daysSinceDelivered = (CoreHelper.SystemTimeNow - latestDelivery.DeliveredAt!.Value).TotalDays;
                    if (daysSinceDelivered < 3)
                    {
                        var remainingDays = Math.Ceiling(3 - daysSinceDelivered);
                        failures.Add(new ValidationFailure(nameof(order.OrderStatus),
                            $"Cần thêm {remainingDays} ngày nữa để hệ thống tự động cho phép nhân viên xác nhận hoàn tất (thời gian khiếu nại của khách)."));
                    }
                }
            }
        }

        // --- KIỂM TRA 2: HÀNG THIẾT KẾ (DESIGN SERVICES) ---
        var hasUnfinishedDesign = order.OrderItems
                .Any(oi => oi.SourceType == SourceTypes.DesignService
                    && oi.FulfillmentStatus != OrderItemStatuses.Finished);
        if (hasUnfinishedDesign)
        {
            failures.Add(new ValidationFailure(nameof(order.OrderItems),
                "Vẫn còn dịch vụ thiết kế chưa hoàn tất bàn giao file cho khách hàng."));
        }
        failures.ThrowIfAny();

        // 1. Chuyển trạng thái Order sang COMPLETED
        order.OrderStatus = OrderStatuses.Completed;
        order.CompletedAt = CoreHelper.SystemTimeNow;
        order.LastModified = CoreHelper.SystemTimeNow;
        order.LastModifiedBy = _user.Username;
        // Nếu khách hàng tự xác nhận khi đang InTransit, ta nên cập nhật luôn Shipment sang Delivered
        if (isOwner && order.Shipments != null)
        {
            // Tìm kiện hàng đang trong quá trình vận chuyển (không lấy những kiện đã CANCELLED hoặc FAILED trước đó)
            var activeShipment = order.Shipments
                .FirstOrDefault(s => s.ShipmentStatus == ShipmentStatuses.InTransit);

            if (activeShipment != null)
            {
                order.DeliveredAt = CoreHelper.SystemTimeNow;
                activeShipment.ShipmentStatus = ShipmentStatuses.Delivered;
                activeShipment.DeliveredAt = CoreHelper.SystemTimeNow.UtcDateTime;
                activeShipment.LastModified = CoreHelper.SystemTimeNow;
                activeShipment.LastModifiedBy = _user.Username;
                //activeShipment.Note += $"\n[{CoreHelper.SystemTimeNow:HH:mm dd/MM}] Khách hàng chủ động xác nhận đã nhận được hàng.";
            }
        }
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException($"Lỗi lưu trạng thái hoàn tất đơn hàng: {ex.Message}");
        }
        return _mapper.Map<OrderDTO>(order);
    }
}
