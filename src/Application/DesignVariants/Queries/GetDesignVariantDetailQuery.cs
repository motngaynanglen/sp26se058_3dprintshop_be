using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

public record GetDesignVariantDetailQuery : IRequest<DesignVariantDetailDTO>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid Id { get; init; }
}

/// <summary>
/// DTO chi tiết variant — bao gồm thông tin template, material, media kế thừa.
/// </summary>
public class DesignVariantDetailDTO
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string? PreviewModelUrl { get; set; }
    public Guid DesignTemplateId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal? SizeScale { get; set; }
    public int StockQuantity { get; set; }
    public int? MinimumStockLevel { get; set; }
    public bool IsAllowPreOrder { get; set; }
    public decimal? EstimatedWeightPerUnit { get; set; }
    public decimal? EstimatedPrintTimePerUnit { get; set; }
    public decimal MarkupPercentage { get; set; }
    public string CatalogStatus { get; set; } = null!;
    public bool IsActive { get; set; }

    // Thông tin template (kế thừa file/ảnh)
    public string? DesignTemplateName { get; set; }
    public string? DesignTemplateCode { get; set; }
    public string? DesignTemplateFileUrl { get; set; }
    public string? DesignTemplateThumbnailUrl { get; set; }

    // Thông tin vật liệu
    public string? MaterialName { get; set; }

    // Media hiệu lực — variant override hoặc kế thừa từ template
    public string? EffectivePreviewModelUrl { get; set; }
    public string? EffectiveThumbnailUrl { get; set; }
}

public class GetDesignVariantDetailQueryHandler : IRequestHandler<GetDesignVariantDetailQuery, DesignVariantDetailDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetDesignVariantDetailQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<DesignVariantDetailDTO> Handle(GetDesignVariantDetailQuery request, CancellationToken cancellationToken)
    {
        var variant = await _context.DesignVariants
            .AsNoTracking()
            .Include(v => v.DesignTemplate)
            .Include(v => v.Material)
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (variant == null)
        {
            throw new DataNotFoundException(nameof(DesignVariant), request.Id);
        }

        // Customer/Guest chỉ thấy variant PUBLISHED
        bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;
        if (!isStaffOrManager)
        {
            if (variant.CatalogStatus != CatalogStatuses.Published || !variant.IsActive)
            {
                throw new DataNotFoundException(nameof(DesignVariant), request.Id);
            }
        }

        return new DesignVariantDetailDTO
        {
            Id = variant.Id,
            Code = variant.Code,
            Name = variant.Name,
            Description = variant.Description,
            Price = variant.Price,
            ImageUrls = TryDeserializeList(variant.ImageUrls),
            PreviewModelUrl = variant.PreviewModelUrl,
            DesignTemplateId = variant.DesignTemplateId,
            MaterialId = variant.MaterialId,
            SizeScale = variant.SizeScale,
            StockQuantity = variant.StockQuantity,
            MinimumStockLevel = variant.MinimumStockLevel,
            IsAllowPreOrder = variant.IsAllowPreOrder,
            EstimatedWeightPerUnit = variant.EstimatedWeightPerUnit,
            EstimatedPrintTimePerUnit = variant.EstimatedPrintTimePerUnit,
            MarkupPercentage = variant.MarkupPercentage,
            CatalogStatus = variant.CatalogStatus,
            IsActive = variant.IsActive,
            // Template
            DesignTemplateName = variant.DesignTemplate?.Name,
            DesignTemplateCode = variant.DesignTemplate?.Code,
            DesignTemplateFileUrl = variant.DesignTemplate?.FileUrl,
            DesignTemplateThumbnailUrl = variant.DesignTemplate?.ThumbnailUrl,
            // Material
            MaterialName = variant.Material?.Name,
            // Effective media — variant override hoặc fallback template
            EffectivePreviewModelUrl = !string.IsNullOrEmpty(variant.PreviewModelUrl)
                ? variant.PreviewModelUrl
                : variant.DesignTemplate?.FileUrl,
            EffectiveThumbnailUrl = variant.DesignTemplate?.ThumbnailUrl,
        };
    }

    private static List<string> TryDeserializeList(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }
}
