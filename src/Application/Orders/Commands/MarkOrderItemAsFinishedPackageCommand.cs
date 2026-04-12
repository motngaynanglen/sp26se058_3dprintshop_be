using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;
[Authorize(Roles =  Roles.STAFF + "," + Roles.MANAGER)]
public record MarkOrderItemAsFinishedPackageCommand : IRequest<object>
{
    [JsonIgnore]
    public Guid Id { get; init; }
}
public class MarkOrderItemAsFinishedCommandHandler : IRequestHandler<MarkOrderItemAsFinishedPackageCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public MarkOrderItemAsFinishedCommandHandler(IApplicationDbContext context,  IUser user, IMapper mapper )
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<object> Handle(MarkOrderItemAsFinishedPackageCommand request, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();

        // 1. Load Item kèm Order và toàn bộ Items khác để check điều kiện gộp
        var item = await _context.OrderItems
            .Include(oi => oi.Order)
                .ThenInclude(o => o.OrderItems)
            .FirstOrDefaultAsync(oi => oi.Id == request.Id, cancellationToken);

        if (item == null)
        {
            throw new DataNotFoundException(nameof(OrderItem), request.Id);
        }

        // 2. Kiểm tra loại sản phẩm (Chỉ dành cho vật lý)
        var physicalTypes = new[] { SourceTypes.InStock, SourceTypes.PreOrder, SourceTypes.PrintService };
        if (!physicalTypes.Contains(item.SourceType))
        {
            failures.AddFailure(nameof(item.SourceType), "Sản phẩm này thuộc luồng thiết kế, không thể xử lý đóng gói vật lý.");
        }
        failures.ThrowIfAny();
        // 3. Cập nhật trạng thái sang FINISHED
        // Nếu đã hoàn thành rồi thì không cho hoàn thành lại để tránh side-effect logic shipment
        if (item.FulfillmentStatus == OrderItemStatuses.Finished)
        {
            throw new BusinessException(
                "Món hàng này đã được đánh dấu hoàn thành từ trước.",
                ResponseCodeConstants.VAL_INVALID_STATE
            );
        }
        item.FulfillmentStatus = OrderItemStatuses.Finished;
        item.LastModified = CoreHelper.SystemTimeNow;
        item.LastModifiedBy = _user.Username;

        // 4. KIỂM TRA TỰ ĐỘNG: Đơn hàng đã đủ điều kiện để giao chưa?
        // Một đơn hàng sẵn sàng giao khi TẤT CẢ các món đều ở trạng thái FINISHED
        bool isOrderReadyToShip = item.Order.OrderItems
            .All(x => x.FulfillmentStatus == OrderItemStatuses.Finished);

        if (isOrderReadyToShip)
        {
            // Cập nhật trạng thái Order tổng
            //item.Order.OrderStatus = OrderStatuses.Finished;

            // Cập nhật ghi chú Shipment để báo cho Staff kho
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.OrderId == item.OrderId, cancellationToken);

            if (shipment != null)
            {
                shipment.ShipmentStatus = ShipmentStatuses.ReadyForPickup;
                shipment.LastModified = CoreHelper.SystemTimeNow;
                shipment.LastModifiedBy = "SYSTEM_AUTO";
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException($"Lỗi lưu trạng thái hoàn thành: {ex.Message}");
        }
        return _mapper.Map<OrderItemDTO>(item);
    }
}
