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
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
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

    public MarkOrderItemAsFinishedCommandHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<object> Handle(MarkOrderItemAsFinishedPackageCommand request, CancellationToken cancellationToken)
    {

        // 1. Load Item kèm Order và toàn bộ Items khác để check điều kiện gộp
        var item = await _context.OrderItems
            .Include(oi => oi.Order)
                .ThenInclude(o => o.OrderItems)
            .FirstOrDefaultAsync(oi => oi.Id == request.Id, cancellationToken);

        if (item == null)
        {
            throw new DataNotFoundException(nameof(OrderItem), request.Id);
        }
        // Nếu đã hoàn thành rồi thì không cho hoàn thành lại để tránh side-effect logic shipment
        if (item.FulfillmentStatus == OrderItemStatuses.Finished)
        {
            throw new BusinessException(
                "Món hàng này đã được đánh dấu hoàn thành từ trước.",
                ResponseCodeConstants.VAL_INVALID_STATE
            );
        }
        // --- PHÂN LUỒNG XỬ LÝ THEO 4 LOẠI ---
        switch (item.SourceType)
        {
            case SourceTypes.InStock:
                // Hàng có sẵn: Chỉ cần đổi trạng thái (vì Stock đã trừ lúc thanh toán/xác nhận)
                item.FulfillmentStatus = OrderItemStatuses.Finished;
                break;

            case SourceTypes.PreOrder:
                // Hàng đặt trước: Cần nghiệp vụ Nhập - Xuất ảo để cân bằng kho (như code cũ của bạn)
                if (item.DesignVariantId.HasValue)
                {
                    var variant = item.DesignVariant;
                    if (variant != null)
                    {
                        // Nhập kho từ xưởng
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            Id = Guid.NewGuid(),
                            DesignVariantId = variant.Id,
                            ReferenceId = item.Id,
                            Quantity = item.QuantityOrdered,
                            Type = InventoryTransactionTypes.ProductionIn,
                            Note = $"[Nhập] Hoàn tất sản xuất đơn đặt trước: {item.Order.Code}",
                            Created = CoreHelper.SystemTimeNow,
                            CreatedBy = _user.Username
                        });

                        // Xuất kho giao khách
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            Id = Guid.NewGuid(),
                            DesignVariantId = variant.Id,
                            ReferenceId = item.Order.Id,
                            Quantity = -item.QuantityOrdered,
                            Type = InventoryTransactionTypes.OrderOut,
                            Note = $"[Xuất] Đóng gói đơn đặt trước: {item.Order.Code}",
                            Created = CoreHelper.SystemTimeNow,
                            CreatedBy = _user.Username
                        });
                    }
                }
                item.FulfillmentStatus = OrderItemStatuses.Finished;
                break;

            case SourceTypes.PrintService:
                // Dịch vụ in theo yêu cầu: Đánh dấu đã in xong và kiểm tra chất lượng (QC) xong
                item.FulfillmentStatus = OrderItemStatuses.Finished;
                break;

            case SourceTypes.DesignService:
                // Dịch vụ thiết kế: "Hoàn thành" ở đây nghĩa là Staff đã upload file cuối cùng 
                // và xác nhận bàn giao quyền sở hữu file cho khách.

                if (item.DesignWorkId.HasValue)
                {
                    var designWork = await _context.DesignWorks
                        .FirstOrDefaultAsync(dw => dw.Id == item.DesignWorkId, cancellationToken);

                    if (designWork == null)
                    {
                        throw new DataNotFoundException(nameof(DesignWork), item.DesignWorkId.Value);
                    }

                    var invalidStatuses = new[] {
                            DesignWorkStatus.Sketching,
                            DesignWorkStatus.Pending,
                            DesignWorkStatus.InProgress
                        };
                    if (invalidStatuses.Contains(designWork.Status))
                    {
                        throw new BusinessException(
                            $"Món hàng '{item.ItemName}' đang ở trạng thái {designWork.Status}. " +
                            "Dự án phải được chuyển sang giai đoạn đang xem xét và có file kết quả mới được hoàn thành.",
                            ResponseCodeConstants.VAL_INVALID_STATE
                        );
                    }

                    if (designWork.ResultDraftId == null || designWork.ResultDraftId == Guid.Empty)
                    {
                        throw new BusinessException(
                            $"Món hàng '{item.ItemName}' chưa có bản thiết kế cuối cùng. Nhân viên cần tải file lên và chọn bản thảo hoàn tất trước khi đóng gói.",
                            ResponseCodeConstants.VAL_INVALID_STATE
                        );
                    }
                   
                    // Cập nhật trạng thái DesignWork 
                    // Khi đã xong, dự án thiết kế phải được khóa để đảm bảo tính Immutable (không sửa đổi lịch sử)
                    if (designWork.Status != DesignWorkStatus.Completed)
                    {
                        designWork.Status = DesignWorkStatus.Completed;
                        //designWork.IsLocked = true; 
                        designWork.LastModified = CoreHelper.SystemTimeNow;
                        designWork.LastModifiedBy = _user.Username;

                        // 4. Ghi log hệ thống xác nhận bàn giao quyền sở hữu
                        _context.DesignLogs.Add(new DesignLog
                        {
                            Id = Guid.NewGuid(),
                            DesignWorkId = designWork.Id,
                            LogType = DesignLogType.System,
                            Content = $"Hệ thống: Bản thiết kế cuối cùng đã được bàn giao cho khách hàng. Trạng thái dịch vụ chuyển sang Hoàn thành.",
                            Created = CoreHelper.SystemTimeNow
                        });
                    }
                }
                item.FulfillmentStatus = OrderItemStatuses.Finished;
                break;

            default:
                throw new NotSupportedException("Loại sản phẩm không hỗ trợ trạng thái hoàn thành này.");
        }

        item.LastModified = CoreHelper.SystemTimeNow;
        item.LastModifiedBy = _user.Username;

        // --- KIỂM TRA ĐIỀU KIỆN SHIPMENT (AUTO WORKFLOW) ---
        // Đơn hàng sẵn sàng giao khi TẤT CẢ các món (bao gồm cả thiết kế) đều Finished
        // Lưu ý: Nếu đơn chỉ có mỗi DesignService thì thường sẽ chuyển trạng thái Order sang Finished luôn 
        // vì không cần Ship vật lý qua kho.

        var allItemsFinished = item.Order.OrderItems.All(x => x.FulfillmentStatus == OrderItemStatuses.Finished);

        if (allItemsFinished)
        {
            // Kiểm tra xem đơn có cần giao vận vật lý không
            var hasPhysicalItem = item.Order.OrderItems.Any(oi =>
                oi.SourceType == SourceTypes.InStock ||
                oi.SourceType == SourceTypes.PreOrder ||
                oi.SourceType == SourceTypes.PrintService);

            if (hasPhysicalItem)
            {
                // Cập nhật Shipment sang ReadyForPickup
                var shipment = await _context.Shipments
                    .FirstOrDefaultAsync(s => s.OrderId == item.OrderId, cancellationToken);

                if (shipment != null && shipment.ShipmentStatus == ShipmentStatuses.Preparing)
                {
                    shipment.ShipmentStatus = ShipmentStatuses.ReadyForPickup;
                    shipment.LastModified = CoreHelper.SystemTimeNow;
                    shipment.LastModifiedBy = "SYSTEM_AUTO";
                }
            }
            else
            {
                // Nếu toàn bộ là DesignService thì đơn hàng coi như hoàn tất (không cần ship)
                item.Order.OrderStatus = OrderStatuses.Completed;
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
