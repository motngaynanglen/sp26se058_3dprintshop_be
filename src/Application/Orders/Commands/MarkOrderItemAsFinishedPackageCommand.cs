using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;
public record MarkOrderItemAsFinishedPackageCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid Id { get; init; }
}
public class MarkOrderItemAsFinishedCommandHandler : IRequestHandler<MarkOrderItemAsFinishedPackageCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public MarkOrderItemAsFinishedCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<bool> Handle(MarkOrderItemAsFinishedPackageCommand request, CancellationToken cancellationToken)
    {
        // 1. Load Item kèm Order và toàn bộ Items khác để check điều kiện gộp
        var item = await _context.OrderItems
            .Include(oi => oi.Order)
                .ThenInclude(o => o.OrderItems)
            .FirstOrDefaultAsync(oi => oi.Id == request.Id, cancellationToken);

        if (item == null) throw new Exception("Không tìm thấy món hàng.");

        // 2. Kiểm tra loại sản phẩm (Chỉ dành cho vật lý)
        var physicalTypes = new[] { SourceTypes.InStock, SourceTypes.PreOrder, SourceTypes.PrintService };
        if (!physicalTypes.Contains(item.SourceType))
        {
            throw new Exception("Sản phẩm thuộc luồng thiết kế, vui lòng sử dụng API thiết kế.");
        }

        // 3. Cập nhật trạng thái sang FINISHED
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
            // item.Order.OrderStatus = OrderStatuses.Finished;

            // Cập nhật ghi chú Shipment để báo cho Staff kho
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.OrderId == item.OrderId, cancellationToken);

            if (shipment != null)
            {
                shipment.ShipmentStatus = ShipmentStatuses.ReadyForPickup;
                //shipment.Note = $"Hoàn thành lúc {DateTime.Now:HH:mm dd/MM}. Sẵn sàng đóng gói.";
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
