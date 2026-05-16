using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Ocsp;
using PayOS.Exceptions;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.InventoryTransactions.Commands;
[Authorize(Roles = Roles.MANAGER + "," + Roles.STAFF)]
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
public class CreateInventoryTransactionCommandValidator : AbstractValidator<CreateInventoryTransactionCommand>
{
    public CreateInventoryTransactionCommandValidator()
    {
        // 1. Kiểm tra mã biến thể không được trống
        RuleFor(v => v.DesignVariantId)
            .NotEmpty().WithMessage("Mã biến thể sản phẩm là bắt buộc.");

        // 2. Kiểm tra số lượng (Quantity) phải khác 0
        RuleFor(v => v.Quantity)
            .NotEqual(0).WithMessage("Số lượng thay đổi phải khác 0.")
            .DependentRules(() =>
            {
                // Chỉ khi Quantity != 0 thì mới check logic âm/dương này
                RuleFor(v => v).Must(v =>
                {
                    if (InventoryTransactionTypes.IsAlwaysPositive(v.Type))
                    {
                        return v.Quantity > 0;
                    }
                    if (InventoryTransactionTypes.IsAlwaysNegative(v.Type))
                    {
                        return v.Quantity < 0;
                    }
                    // Adjustment thì cho phép cả hai
                    return true;
                })
                .WithMessage(v => $"Loại giao dịch {v.Type} chỉ chấp nhận số lượng dương.")
                .WithName(nameof(CreateInventoryTransactionCommand.Quantity));
            });

        // 3. Kiểm tra loại giao dịch (Type)
        RuleFor(v => v.Type)
            .NotEmpty().WithMessage("Loại giao dịch là bắt buộc.")
            .Must(type => new[] {
                InventoryTransactionTypes.PurchaseIn,
                InventoryTransactionTypes.ProductionIn,
                InventoryTransactionTypes.Adjustment
            }.Contains(type))
            .WithMessage("Loại giao dịch không hợp lệ.");

        // 4. Kiểm tra độ dài ghi chú (nếu có)
        RuleFor(v => v.Note)
            .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }
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
        var userRole = _user.Role;
        var userId = _user.Id.ToGuid();

        var staff = await _context.Staffs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == userId, ct);

        if (staff == null && _user.Role == Roles.STAFF)
            throw new ForbiddenAccessException("Thông tin nhân viên không tồn tại.");


        // Tìm biến thể và cập nhật kho
        var variant = await _context.DesignVariants
         .FirstOrDefaultAsync(x => x.Id == request.DesignVariantId, ct);

        if (variant == null)
        {
            throw new DataNotFoundException(nameof(DesignVariant), request.DesignVariantId);
        }

        int newQuantity = variant.StockQuantity + request.Quantity;
        if (newQuantity < 0)
        {
            throw new BadRequestException($"Kho không đủ hàng. (Hiện có: {variant.StockQuantity})");
        }

        variant.StockQuantity = newQuantity;
        // 4. Tạo Log giao dịch InventoryTransaction
        var entity = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            DesignVariantId = request.DesignVariantId,
            Quantity = request.Quantity,
            Type = request.Type,
            Note = request.Note,
            StaffId = staff?.Id // Có thể null nếu là Manager thực hiện mà không có bản ghi Staff
        };

        _context.InventoryTransactions.Add(entity);


        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException($"{ex.InnerException?.Message ?? ex.Message}");
        }
        return request;
    }
}
