using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignVariant.Queries;   // ← Thêm using này

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
}

public class CreateDesignVariantCommandHandler : IRequestHandler<CreateDesignVariantCommand, DesignVariantDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;                    // ← Thêm IMapper

    public CreateDesignVariantCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<DesignVariantDTO> Handle(CreateDesignVariantCommand request, CancellationToken cancellationToken)
    {
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

        var newDesignVariant = new Domain.Entities.DesignVariant
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
            IsActive = true,                          // ← Nên set mặc định
            Created = DateTime.UtcNow
        };

        _context.DesignVariants.Add(newDesignVariant);
        await _context.SaveChangesAsync(cancellationToken);

        // Trả về DTO
        var dto = _mapper.Map<DesignVariantDTO>(newDesignVariant);
        return dto;
    }
}
