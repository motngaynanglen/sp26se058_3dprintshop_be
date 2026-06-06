using System;
using System.ComponentModel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

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
    /// <summary>Để trống/null = dùng file 3D của mẫu; có giá trị = override riêng.</summary>
    public string? PreviewModelUrl { get; init; }
    public bool? ClearPreviewOverride { get; init; }
    /// <summary>Để trống/null = dùng ảnh của mẫu; có giá trị = override riêng.</summary>
    public string? PreviewImageUrl { get; init; }
    public bool? ClearPreviewImageOverride { get; init; }
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
            .IgnoreQueryFilters()
            .Include(x => x.DesignTemplate)
            .Include(x => x.Material)
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

        if (request.Price.HasValue)
            entity.Price = request.Price.Value;

        if (request.IsAllowPreOrder.HasValue)
            entity.IsAllowPreOrder = request.IsAllowPreOrder.Value;

        if (request.EstimatedWeightPerUnit.HasValue)
            entity.EstimatedWeightPerUnit = request.EstimatedWeightPerUnit.Value;

        if (request.EstimatedPrintTimePerUnit.HasValue)
            entity.EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit.Value;

        if (request.ClearPreviewOverride == true)
            entity.PreviewModelUrl = null;
        else if (request.PreviewModelUrl != null)
            entity.PreviewModelUrl = string.IsNullOrWhiteSpace(request.PreviewModelUrl)
                ? null
                : request.PreviewModelUrl.Trim();

        if (request.ClearPreviewImageOverride == true)
            entity.PreviewImageUrl = null;
        else if (request.PreviewImageUrl != null)
            entity.PreviewImageUrl = string.IsNullOrWhiteSpace(request.PreviewImageUrl)
                ? null
                : request.PreviewImageUrl.Trim();

        entity.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<DesignVariantDTO>(entity);
        return dto;
    }
}
