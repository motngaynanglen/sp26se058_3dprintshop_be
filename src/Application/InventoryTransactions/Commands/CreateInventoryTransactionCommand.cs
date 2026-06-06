using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.InventoryTransactions.Commands;
public record CreateInventoryTransactionCommand : IRequest<CreateInventoryTransactionCommand>
{
    public Guid DesignVariantId { get; init; }
    [DefaultValue(0)]
    public int Quantity { get; init; } // Số dương là Nhập, số âm là Xuất/Điều chỉnh giảm
    [DefaultValue(InventoryTransactionTypes.Adjustment)]
    public required string Type { get; init; } // 'PurchaseIn', 'ProductionIn', 'Adjustment'
    public string? Note { get; init; }
    //public Guid? ReferenceId { get; init; }
}

public class CreateInventoryTransactionHandler : IRequestHandler<CreateInventoryTransactionCommand, CreateInventoryTransactionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateInventoryTransactionHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<CreateInventoryTransactionCommand> Handle(CreateInventoryTransactionCommand request, CancellationToken ct)
    {
        // 1. Kiểm tra quyền truy cập (Staff hoặc Manager)
        var userRole = _user.Role;
        var userId = _user.Id.ToGuid();

        // Kiểm tra: Nếu Role KHÔNG PHẢI Staff VÀ cũng KHÔNG PHẢI Manager thì chặn
        if (userRole != Roles.STAFF && userRole != Roles.MANAGER && userRole != Roles.ADMIN)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện chức năng này.");
        }

        // 2. Lấy thông tin Staff từ DB để lấy StaffId thực tế
        var staff = await _context.Staffs
            .FirstOrDefaultAsync(x => x.AccountId == userId, ct);

        if (staff == null && userRole == Roles.STAFF)
        {
            throw new Exception("Thông tin nhân viên không tồn tại trong hệ thống.");
        }

        // 3. Tìm biến thể và cập nhật kho
        var variant = await _context.DesignVariants
            .FirstOrDefaultAsync(x => x.Id == request.DesignVariantId, ct)
            ?? throw new Exception("Không tìm thấy biến thể sản phẩm.");


        variant.StockQuantity += request.Quantity;

        if (variant.StockQuantity < 0)
            throw new Exception("Số lượng tồn kho không thể âm sau khi điều chỉnh.");


        // 4. Tạo Log giao dịch InventoryTransaction
        var entity = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            DesignVariantId = request.DesignVariantId,
            Quantity = request.Quantity,
            Type = request.Type,
            //ReferenceId = request.ReferenceId,
            Note = request.Note,
            StaffId = staff?.Id // Có thể null nếu là Manager thực hiện mà không có bản ghi Staff
        };

        _context.InventoryTransactions.Add(entity);

        // Lưu thay đổi (Bao gồm cả StockQuantity của Variant và Log mới)
        await _context.SaveChangesAsync(ct);

        return request;
    }
}
