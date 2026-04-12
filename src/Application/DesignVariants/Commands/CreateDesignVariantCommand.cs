using System;
using System.ComponentModel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;   // ← Thêm using này

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record CreateDesignVariantCommand : IRequest<DesignVariantDTO>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid DesignTemplateId { get; init; }
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid MaterialId { get; init; }
    [DefaultValue("VAR-001")]
    public string Code { get; init; } = null!;
    [DefaultValue("Biến thể mặc định")]
    public string Name { get; init; } = null!;
    [DefaultValue(1.0)]
    public decimal SizeScale { get; init; }
    [DefaultValue(100)]
    public int StockQuantity { get; init; }
    [DefaultValue(50000.0)]
    public decimal Price { get; init; }
    [DefaultValue(true)]
    public bool IsAllowPreOrder { get; init; }
    [DefaultValue(0.5)]
    public decimal EstimatedWeightPerUnit { get; init; }
    [DefaultValue(120.0)]
    public decimal EstimatedPrintTimePerUnit { get; init; }
    [DefaultValue(true)]
    public bool IsActive { get; init; } = true;
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
        // 1. Kiểm tra DesignTemplate tồn tại (Sử dụng NotFoundException - DB_004)
        var templateExists = await _context.DesignTemplates
            .AnyAsync(t => t.Id == request.DesignTemplateId, cancellationToken);

        if (!templateExists)
            throw new DataNotFoundException(nameof(DesignTemplate), request.DesignTemplateId);
        // 2. Kiểm tra Material tồn tại (Sử dụng DataNotFoundException - DB_004)
        var materialExists = await _context.Materials
            .AnyAsync(m => m.Id == request.MaterialId, cancellationToken);

        if (!materialExists)
            throw new DataNotFoundException(nameof(Material), request.MaterialId);
        // 3. Kiểm tra trùng lặp Code của Variant (Sử dụng DuplicateException - DB_001)
        var codeExists = await _context.DesignVariants
            .AnyAsync(dv => dv.Code == request.Code, cancellationToken);

        if (codeExists)
            throw new DuplicateException(nameof(DesignVariant), nameof(request.Code), request.Code);
        
        var newDesignVariant = new DesignVariant
        {
            DesignTemplateId = request.DesignTemplateId,
            MaterialId = request.MaterialId,
            Code = request.Code,
            Name = request.Name,
            SizeScale = request.SizeScale,
            StockQuantity = request.StockQuantity,
            Price = request.Price,
            IsAllowPreOrder = request.IsAllowPreOrder,
            EstimatedWeightPerUnit = request.EstimatedWeightPerUnit,
            EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit,
            IsActive = request.IsActive,                          
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };
        _context.DesignVariants.Add(newDesignVariant);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Ném lỗi Create Failure nếu có vấn đề phát sinh từ DB (DB_003)
            throw new CreateFailureException(nameof(DesignVariant), ex.Message);
        }
        return _mapper.Map<DesignVariantDTO>(newDesignVariant);
    }
}
