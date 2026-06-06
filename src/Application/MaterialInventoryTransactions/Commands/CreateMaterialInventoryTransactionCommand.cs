using System.ComponentModel;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.MaterialInventoryTransactions.Commands;

public record CreateMaterialInventoryTransactionCommand : IRequest<CreateMaterialInventoryTransactionCommand>
{
    public Guid MaterialId { get; init; }

    /// <summary>Số gram — dương là nhập, âm là xuất/điều chỉnh giảm.</summary>
    [DefaultValue(0)]
    public decimal QuantityGrams { get; init; }

    [DefaultValue(MaterialInventoryTransactionTypes.PurchaseIn)]
    public required string Type { get; init; }

    public string? Note { get; init; }
}

public class CreateMaterialInventoryTransactionHandler
    : IRequestHandler<CreateMaterialInventoryTransactionCommand, CreateMaterialInventoryTransactionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateMaterialInventoryTransactionHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<CreateMaterialInventoryTransactionCommand> Handle(
        CreateMaterialInventoryTransactionCommand request,
        CancellationToken ct)
    {
        var userRole = _user.Role;
        var userId = _user.Id.ToGuid();

        if (userRole != Roles.STAFF && userRole != Roles.MANAGER && userRole != Roles.ADMIN)
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện chức năng này.");

        if (request.QuantityGrams == 0)
            throw new InvalidOperationException("Số lượng gram phải khác 0.");

        var staff = await _context.Staffs
            .FirstOrDefaultAsync(x => x.AccountId == userId, ct);

        if (staff == null && userRole == Roles.STAFF)
            throw new InvalidOperationException("Thông tin nhân viên không tồn tại trong hệ thống.");

        var material = await _context.Materials
            .FirstOrDefaultAsync(x => x.Id == request.MaterialId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy vật liệu.");

        if (!material.IsActive && request.QuantityGrams > 0)
            throw new InvalidOperationException("Vật liệu đã ngừng kinh doanh — không thể nhập kho.");

        material.StockQuantityGrams += request.QuantityGrams;

        if (material.StockQuantityGrams < 0)
            throw new InvalidOperationException("Tồn kho vật liệu không thể âm sau khi điều chỉnh.");

        _context.MaterialInventoryTransactions.Add(new MaterialInventoryTransaction
        {
            Id = Guid.NewGuid(),
            MaterialId = request.MaterialId,
            QuantityGrams = request.QuantityGrams,
            Type = request.Type,
            Note = request.Note,
            StaffId = staff?.Id
        });

        await _context.SaveChangesAsync(ct);
        return request;
    }
}
