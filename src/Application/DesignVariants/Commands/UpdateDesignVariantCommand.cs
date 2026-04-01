using System;
using System.ComponentModel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignVariant.Queries;

namespace sp26se058_3dprintshop_be.Application.DesignVariant.Commands;

public record UpdateDesignVariantCommand : IRequest<DesignVariantDTO>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; init; }
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
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

public class UpdateDesignVariantCommandHandler : IRequestHandler<UpdateDesignVariantCommand, DesignVariantDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateDesignVariantCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<DesignVariantDTO> Handle(UpdateDesignVariantCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.DesignVariants
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
            throw new Exception("Không tìm thấy biến thể thiết kế");

        // Update only fields that are provided (nullable)
        if (request.MaterialId.HasValue)
            entity.MaterialId = request.MaterialId.Value;

        if (request.Code != null)
            entity.Code = request.Code;

        if (request.Name != null)
            entity.Name = request.Name;

        if (request.SizeScale.HasValue)
            entity.SizeScale = request.SizeScale.Value;

        if (request.StockQuantity.HasValue)
            entity.StockQuantity = request.StockQuantity.Value;

        if (request.Price.HasValue)
            entity.Price = request.Price.Value;

        if (request.IsAllowPreOrder.HasValue)
            entity.IsAllowPreOrder = request.IsAllowPreOrder.Value;

        if (request.EstimatedWeightPerUnit.HasValue)
            entity.EstimatedWeightPerUnit = request.EstimatedWeightPerUnit.Value;

        if (request.EstimatedPrintTimePerUnit.HasValue)
            entity.EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit.Value;

        entity.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<DesignVariantDTO>(entity);
        return dto;
    }
}
