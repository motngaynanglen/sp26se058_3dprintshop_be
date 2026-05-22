using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

[Authorize(Roles = Roles.MANAGER)]
public record UpdateMaterialPriceCommand : IRequest<MaterialDTO>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid MaterialId { get; init; }

    [DefaultValue(2.5)]
    public decimal BaseCostPerGram { get; init; }

    [DefaultValue(5.0)]
    public decimal TotalServiceCostPerGram { get; init; }

    [DefaultValue("2026-05-22")]
    public DateTime EffectiveDate { get; init; }
}

public class UpdateMaterialPriceCommandValidator : AbstractValidator<UpdateMaterialPriceCommand>
{
    public UpdateMaterialPriceCommandValidator()
    {
        RuleFor(x => x.MaterialId)
            .NotEmpty().WithMessage("Vật liệu không hợp lệ.");

        RuleFor(x => x.BaseCostPerGram)
            .GreaterThan(0).WithMessage("Giá nhập phải lớn hơn 0.");

        RuleFor(x => x.TotalServiceCostPerGram)
            .GreaterThanOrEqualTo(x => x.BaseCostPerGram)
            .WithMessage("Giá dịch vụ phải lớn hơn hoặc bằng giá nhập.");

        RuleFor(x => x.EffectiveDate)
            .Must(date => date != default)
            .WithMessage("Ngày hiệu lực không được để trống.");
    }
}

public class UpdateMaterialPriceCommandHandler : IRequestHandler<UpdateMaterialPriceCommand, MaterialDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    private readonly IPricingEngine _pricingEngine;
    private readonly ILogger<UpdateMaterialPriceCommandHandler> _logger;

    public UpdateMaterialPriceCommandHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IUser user,
        IPricingEngine pricingEngine,
        ILogger<UpdateMaterialPriceCommandHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
        _pricingEngine = pricingEngine;
        _logger = logger;
    }

    public async Task<MaterialDTO> Handle(UpdateMaterialPriceCommand request, CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .Include(m => m.PriceHistories)
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId, cancellationToken);

        if (material == null)
        {
            throw new DataNotFoundException(nameof(Material), request.MaterialId);
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var now = CoreHelper.SystemTimeNow;

        var currentPrices = await _context.MaterialPriceHistories
            .Where(x => x.MaterialId == request.MaterialId && x.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var currentPrice in currentPrices)
        {
            currentPrice.IsCurrent = false;
            currentPrice.LastModified = now;
            currentPrice.LastModifiedBy = _user.Username;
        }

        var newPrice = new MaterialPriceHistory
        {
            MaterialId = request.MaterialId,
            BaseCostPerGram = request.BaseCostPerGram,
            TotalServiceCostPerGram = request.TotalServiceCostPerGram,
            EffectiveDate = request.EffectiveDate,
            IsCurrent = true,
            Created = now,
            CreatedBy = _user.Username,
            LastModified = now,
            LastModifiedBy = _user.Username,
        };

        material.IsActive = true;
        material.LastModified = now;
        material.LastModifiedBy = _user.Username;

        _context.MaterialPriceHistories.Add(newPrice);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw new UpdateFailureException(nameof(Material), ex.Message);
        }

        // Idea 2: Auto-sync DesignVariant.Price for variants using this material.
        // Best-effort — failure must NOT roll back the material price update.
        await SyncDesignVariantPricesAsync(request.MaterialId, request.TotalServiceCostPerGram, cancellationToken);

        return await _context.Materials
            .AsNoTracking()
            .Include(x => x.PriceHistories)
            .Where(x => x.Id == request.MaterialId)
            .ProjectTo<MaterialDTO>(_mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
    }

    private async Task SyncDesignVariantPricesAsync(
        Guid materialId,
        decimal newPricePerGram,
        CancellationToken ct)
    {
        try
        {
            var variants = await _context.DesignVariants
                .Where(v => v.MaterialId == materialId
                    && v.IsActive
                    && v.EstimatedWeightPerUnit != null
                    && v.EstimatedWeightPerUnit > 0
                    && v.CatalogStatus != CatalogStatuses.Archived)
                .ToListAsync(ct);

            var now = CoreHelper.SystemTimeNow;
            int syncedCount = 0;

            foreach (var variant in variants)
            {
                var newPrice = _pricingEngine.CalculatePrintCost(
                    variant.EstimatedWeightPerUnit!.Value,
                    newPricePerGram,
                    variant.MarkupPercentage);

                if (newPrice <= 0)
                {
                    _logger.LogWarning(
                        "Skipping DesignVariant {VariantId}: calculated price {Price} is not positive.",
                        variant.Id, newPrice);
                    continue;
                }

                variant.Price = newPrice;
                variant.LastModified = now;
                variant.LastModifiedBy = _user.Username ?? "SYSTEM";
                syncedCount++;
            }

            if (syncedCount > 0)
            {
                await _context.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Material price updated for {MaterialId}. Auto-synced {Count} DesignVariant prices.",
                materialId, syncedCount);
        }
        catch (Exception ex)
        {
            // Sync failure must NOT affect the material price update that already committed
            _logger.LogError(ex,
                "Failed to auto-sync DesignVariant prices for material {MaterialId}. Main price update was NOT rolled back.",
                materialId);
        }
    }
}
