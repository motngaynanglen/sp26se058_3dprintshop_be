using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.DesignVariant.Commands;

public record UpdateDesignVariantCommand : IRequest<Guid>
{
    public Guid Id { get; init; }                 // id của variant cần update

    public Guid? MaterialId { get; init; }

    public string? Code { get; init; }
    public string? Name { get; init; }

    public decimal? SizeScale { get; init; }
    public int? StockQuantity { get; init; }
    public decimal? Price { get; init; }
    public bool? IsAllowPreOrder { get; init; }
    public decimal? EstimatedWeightPerUnit { get; init; }
    public decimal? EstimatedPrintTimePerUnit { get; init; }
}

public class UpdateDesignVariantCommandHandler : IRequestHandler<UpdateDesignVariantCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public UpdateDesignVariantCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<Guid> Handle(UpdateDesignVariantCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.DesignVariants
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
            throw new Exception("Không tìm thấy biến thể thiết kế");

        if (request.MaterialId != null)
            entity.MaterialId = request.MaterialId.Value;

        if (request.Code != null)
            entity.Code = request.Code;

        if (request.Name != null)
            entity.Name = request.Name;

        if (request.SizeScale != null)
            entity.SizeScale = request.SizeScale.Value;

        if (request.StockQuantity != null)
            entity.StockQuantity = request.StockQuantity.Value;

        if (request.Price != null)
            entity.Price = request.Price.Value;

        if (request.IsAllowPreOrder != null)
            entity.IsAllowPreOrder = request.IsAllowPreOrder.Value;

        if (request.EstimatedWeightPerUnit != null)
            entity.EstimatedWeightPerUnit = request.EstimatedWeightPerUnit.Value;

        if (request.EstimatedPrintTimePerUnit != null)
            entity.EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit.Value;

        entity.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

}
