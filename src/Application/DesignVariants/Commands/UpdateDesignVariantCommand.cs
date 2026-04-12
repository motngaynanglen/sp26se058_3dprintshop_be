using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record UpdateDesignVariantCommand : IRequest<DesignVariantDTO>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid Id { get; init; }
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid? MaterialId { get; init; }
    [DefaultValue("VAR-002")]
    public string? Code { get; init; }
    [DefaultValue("Biến thể mặc định 2")]
    public string? Name { get; init; }
    [DefaultValue(1.0)]
    public decimal? SizeScale { get; init; }
    [DefaultValue(100)]
    public int? StockQuantity { get; init; }
    [DefaultValue(50000.0)]
    public decimal? Price { get; init; }
    [DefaultValue(false)]
    public bool? IsAllowPreOrder { get; init; }
    [DefaultValue(0.5)]
    public decimal? EstimatedWeightPerUnit { get; init; }
    [DefaultValue(120.0)]
    public decimal? EstimatedPrintTimePerUnit { get; init; }
    [DefaultValue(true)]
    public bool? IsActive { get; init; } = true;
}

public class UpdateDesignVariantCommandHandler : IRequestHandler<UpdateDesignVariantCommand, DesignVariantDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public UpdateDesignVariantCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignVariantDTO> Handle(UpdateDesignVariantCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm bản ghi hiện tại (Dùng DataNotFoundException - DB_004)
        var entity = await _context.DesignVariants
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
            throw new DataNotFoundException(nameof(DesignVariant), request.Id);

        // 2. Nếu có cập nhật MaterialId, kiểm tra Material đó có tồn tại không
        if (request.MaterialId.HasValue && request.MaterialId.Value != entity.MaterialId)
        {
            var materialExists = await _context.Materials
                .AnyAsync(m => m.Id == request.MaterialId.Value, cancellationToken);

            if (!materialExists)
                throw new DataNotFoundException(nameof(Material), request.MaterialId.Value);

            entity.MaterialId = request.MaterialId.Value;
        }

        // 3. Nếu có cập nhật Code, kiểm tra trùng lặp (DuplicateException - DB_001)
        if (!string.IsNullOrEmpty(request.Code) && request.Code != entity.Code)
        {
            var codeExists = await _context.DesignVariants
                .AnyAsync(dv => dv.Code == request.Code && dv.Id != request.Id, cancellationToken);

            if (codeExists)
                throw new DuplicateException(nameof(DesignVariant), nameof(request.Code), request.Code);

            entity.Code = request.Code;
        }

        // Update only fields that are provided (nullable)
        if (request.Name != null) entity.Name = request.Name;
        if (request.SizeScale.HasValue) entity.SizeScale = request.SizeScale.Value;
        if (request.StockQuantity.HasValue) entity.StockQuantity = request.StockQuantity.Value;
        if (request.Price.HasValue) entity.Price = request.Price.Value;
        if (request.IsAllowPreOrder.HasValue) entity.IsAllowPreOrder = request.IsAllowPreOrder.Value;
        if (request.EstimatedWeightPerUnit.HasValue) entity.EstimatedWeightPerUnit = request.EstimatedWeightPerUnit.Value;
        if (request.EstimatedPrintTimePerUnit.HasValue) entity.EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException(nameof(DesignVariant), ex.Message);
        }

        return _mapper.Map<DesignVariantDTO>(entity);
    }
}
