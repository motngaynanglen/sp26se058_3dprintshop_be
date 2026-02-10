using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.DesignVariant.Commands;

public record CreateDesignVariantCommand : IRequest<Guid>
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

public class CreateDesignVariantCommandHandler : IRequestHandler<CreateDesignVariantCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public CreateDesignVariantCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<Guid> Handle(CreateDesignVariantCommand request, CancellationToken cancellationToken)
    {
        var existTemplate = await _context.DesignTemplates.FindAsync(new object[] { request.DesignTemplateId }, cancellationToken);
        if (existTemplate == null)
        {
            throw new Exception($"Design Template with Id {request.DesignTemplateId} not found.");
        }

        var existMaterial = await _context.Materials.FindAsync(new object[] { request.MaterialId }, cancellationToken);
        if (existMaterial == null)
        {
            throw new Exception($"Material with Id {request.MaterialId} not found.");
        }
        var newDesignVariant = new Domain.Entities.DesignVariant
        {
            DesignTemplateId = existTemplate.Id,
            MaterialId = existMaterial.Id,
            Code = request.Code,
            Name = request.Name,
            SizeScale = request.SizeScale,
            StockQuantity = request.StockQuantity,
            Price = request.Price,
            IsAllowPreOrder = request.IsAllowPreOrder,
            EstimatedWeightPerUnit = request.EstimatedWeightPerUnit,
            EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit,
            Created = DateTime.UtcNow
        };
        _context.DesignVariants.Add(newDesignVariant);
        await _context.SaveChangesAsync(cancellationToken);
        return newDesignVariant.Id;
    }
}
