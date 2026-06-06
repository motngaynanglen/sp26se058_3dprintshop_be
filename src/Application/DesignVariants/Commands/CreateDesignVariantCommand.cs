using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.DesignVariant.Commands;

public record CreateDesignVariantCommand : IRequest<DesignVariantDTO>
{
    public Guid DesignTemplateId { get; init; }
    public Guid MaterialId { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public decimal SizeScale { get; init; }
    public int StockQuantity { get; init; }
    public decimal Price { get; init; }
    public bool IsAllowPreOrder { get; init; }
    public decimal EstimatedWeightPerUnit { get; init; }
    public decimal EstimatedPrintTimePerUnit { get; init; }
    public string? PreviewModelUrl { get; init; }
    public string? PreviewImageUrl { get; init; }
}

public class CreateDesignVariantCommandHandler : IRequestHandler<CreateDesignVariantCommand, DesignVariantDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CreateDesignVariantCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignVariantDTO> Handle(CreateDesignVariantCommand request, CancellationToken cancellationToken)
    {
        if (request.StockQuantity < 1)
            throw new Exception("Vui lòng nhập số lượng nhập kho ban đầu (ít nhất 1 sản phẩm).");

        // Kiểm tra DesignTemplate tồn tại
        var existTemplate = await _context.DesignTemplates
            .FindAsync(new object[] { request.DesignTemplateId }, cancellationToken);

        if (existTemplate == null)
            throw new Exception($"Design Template with Id {request.DesignTemplateId} not found.");

        // Kiểm tra Material tồn tại
        var existMaterial = await _context.Materials
            .FindAsync(new object[] { request.MaterialId }, cancellationToken);

        if (existMaterial == null)
            throw new Exception($"Material with Id {request.MaterialId} not found.");

        var initialStock = request.StockQuantity;
        var userId = _user.Id.ToGuid();
        var staff = await _context.Staffs
            .FirstOrDefaultAsync(x => x.AccountId == userId, cancellationToken);

        var newDesignVariant = new Domain.Entities.DesignVariant
        {
            Id = Guid.NewGuid(),
            DesignTemplateId = request.DesignTemplateId,
            MaterialId = request.MaterialId,
            Code = request.Code,
            Name = request.Name,
            SizeScale = request.SizeScale,
            StockQuantity = initialStock,
            Price = request.Price,
            IsAllowPreOrder = request.IsAllowPreOrder,
            EstimatedWeightPerUnit = request.EstimatedWeightPerUnit,
            EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit,
            PreviewModelUrl = string.IsNullOrWhiteSpace(request.PreviewModelUrl)
                ? null
                : request.PreviewModelUrl.Trim(),
            PreviewImageUrl = string.IsNullOrWhiteSpace(request.PreviewImageUrl)
                ? null
                : request.PreviewImageUrl.Trim(),
            IsActive = true,
            Created = DateTime.UtcNow
        };

        _context.DesignVariants.Add(newDesignVariant);

        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            DesignVariantId = newDesignVariant.Id,
            Quantity = initialStock,
            Type = InventoryTransactionTypes.ProductionIn,
            Note = "Nhập kho ban đầu khi tạo biến thể",
            StaffId = staff?.Id,
        });

        await _context.SaveChangesAsync(cancellationToken);

        var saved = await _context.DesignVariants
            .AsNoTracking()
            .Include(v => v.DesignTemplate)
            .Include(v => v.Material)
            .FirstAsync(v => v.Id == newDesignVariant.Id, cancellationToken);

        return _mapper.Map<DesignVariantDTO>(saved);
    }
}
