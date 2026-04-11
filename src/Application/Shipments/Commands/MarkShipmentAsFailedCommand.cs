using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Shipments.Commands;
public record MarkShipmentAsFailedCommand : IRequest<object>
{
    public Guid Id { get; init; }
    public string Reason { get; init; } = null!; // Lý do thất bại
}
public class MarkShipmentAsFailedCommandHandler : IRequestHandler<MarkShipmentAsFailedCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public MarkShipmentAsFailedCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<object> Handle(MarkShipmentAsFailedCommand request, CancellationToken ct)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (shipment == null) throw new Exception("Không tìm thấy thông tin vận chuyển.");

        // Chỉ có thể thất bại khi đang đi giao
        if (shipment.ShipmentStatus != ShipmentStatuses.InTransit)
        {
            throw new Exception("Trạng thái vận chuyển hiện tại không hợp lệ để báo thất bại.");
        }

        // 1. Cập nhật trạng thái Shipment
        shipment.ShipmentStatus = ShipmentStatuses.Failed;
        // Lưu lý do vào Note để Staff sau này xem lại
        //shipment.Note = $"Giao hàng thất bại lúc {DateTime.Now:HH:mm dd/MM}. Lý do: {request.Reason}";
        shipment.LastModified = CoreHelper.SystemTimeNow;
        shipment.LastModifiedBy = _user.Username;

        // 2. Xử lý Order (Tùy chọn)
        // Thông thường, chúng ta giữ đơn ở Processing để Staff có thể:
        // - Hoặc là bấm "Giao lại" (API MarkReady) 
        // - Hoặc là bấm "Hủy đơn" (API Cancel)

        await _context.SaveChangesAsync(ct);
        return true;
    }
}
