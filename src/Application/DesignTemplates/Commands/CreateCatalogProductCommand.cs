using System.ComponentModel;
using System.Text.Json;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;

public class CatalogVariantRequest
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid MaterialId { get; init; }
    [DefaultValue("VAR-001")]
    public string Code { get; init; } = null!;
    [DefaultValue("Biến thể PLA trắng")]
    public string Name { get; init; } = null!;
    [DefaultValue("Biến thể in bằng vật liệu PLA màu trắng")]
    public string? Description { get; init; }
    [DefaultValue(1.0)]
    public decimal? SizeScale { get; init; }
    [DefaultValue(10)]
    public int StockQuantity { get; init; }
    [DefaultValue(5)]
    public int? MinimumStockLevel { get; init; }
    [DefaultValue(50000.0)]
    public decimal Price { get; init; }
    public List<string>? ImageUrls { get; init; }
    [DefaultValue("https://example.com/preview-model.stl")]
    public string? PreviewModelUrl { get; init; }
    [DefaultValue(true)]
    public bool IsAllowPreOrder { get; init; } = true;
    [DefaultValue(25.5)]
    public decimal? EstimatedWeightPerUnit { get; init; }
    [DefaultValue(120.0)]
    public decimal? EstimatedPrintTimePerUnit { get; init; }
    [DefaultValue(0)]
    public decimal MarkupPercentage { get; init; }
    [DefaultValue(CatalogStatuses.Draft)]
    public string? CatalogStatus { get; init; }
}

public class CatalogTagRequest
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid ConceptTagId { get; init; }
    [DefaultValue(false)]
    public bool IsMainTag { get; init; }
}

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record CreateCatalogProductCommand : IRequest<DesignTemplateDTO>
{
    [DefaultValue("CODE_DEFAULT")]
    public string Code { get; init; } = null!;
    [DefaultValue("Product Name")]
    public string Name { get; init; } = null!;
    [DefaultValue("Description here")]
    public string? Description { get; init; }
    [DefaultValue("https://example.com/file.stl")]
    public string FileUrl { get; init; } = null!;
    [DefaultValue("https://example.com/thumbnail.png")]
    public string? ThumbnailUrl { get; init; }
    public List<CatalogVariantRequest> Variants { get; init; } = new();
    public List<CatalogTagRequest> Tags { get; init; } = new();
}

public class CatalogVariantRequestValidator : AbstractValidator<CatalogVariantRequest>
{
    public CatalogVariantRequestValidator()
    {
        RuleFor(x => x.MaterialId)
            .NotEmpty().WithMessage("Vật liệu của biến thể không hợp lệ.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã biến thể không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên biến thể không được để trống.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá biến thể phải lớn hơn 0.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Số lượng tồn kho không được âm.");

        RuleFor(x => x.MinimumStockLevel)
            .GreaterThanOrEqualTo(0).When(x => x.MinimumStockLevel.HasValue)
            .WithMessage("Mức tồn kho tối thiểu không được âm.");

        RuleFor(x => x.SizeScale)
            .GreaterThan(0).When(x => x.SizeScale.HasValue)
            .WithMessage("Tỉ lệ kích thước phải lớn hơn 0.");

        RuleFor(x => x.EstimatedWeightPerUnit)
            .GreaterThan(0).When(x => x.EstimatedWeightPerUnit.HasValue)
            .WithMessage("Khối lượng ước tính phải lớn hơn 0.");

        RuleFor(x => x.EstimatedPrintTimePerUnit)
            .GreaterThan(0).When(x => x.EstimatedPrintTimePerUnit.HasValue)
            .WithMessage("Thời gian in ước tính phải lớn hơn 0.");

        RuleFor(x => x.MarkupPercentage)
            .GreaterThanOrEqualTo(0).WithMessage("Phần trăm phụ thu không được âm.");

        RuleFor(x => x.CatalogStatus)
            .Must(x => string.IsNullOrWhiteSpace(x) || CatalogStatuses.IsValid(x))
            .WithMessage("Trạng thái catalog không hợp lệ.");

        RuleFor(x => x.ImageUrls)
            .Must(x => x == null || x.All(url => !string.IsNullOrWhiteSpace(url)))
            .WithMessage("Đường dẫn ảnh biến thể không được để trống.");
    }
}

public class CreateCatalogProductCommandValidator : AbstractValidator<CreateCatalogProductCommand>
{
    public CreateCatalogProductCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã mẫu thiết kế không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên mẫu thiết kế không được để trống.");

        RuleFor(x => x.FileUrl)
            .NotEmpty().WithMessage("File thiết kế không được để trống.");

        RuleFor(x => x.Variants)
            .NotEmpty().WithMessage("Sản phẩm phải có ít nhất một biến thể.");

        RuleForEach(x => x.Variants)
            .SetValidator(new CatalogVariantRequestValidator());

        RuleFor(x => x.Tags)
            .Must(x => x == null || x.Count(tag => tag.IsMainTag) <= 1)
            .WithMessage("Chỉ được phép có đúng 1 tag chính.");

        RuleFor(x => x.Variants)
            .Must(HaveUniqueVariantCodes)
            .WithMessage("Danh sách biến thể có mã bị trùng.");

        RuleFor(x => x.Tags)
            .Must(HaveUniqueTagIds)
            .WithMessage("Danh sách tag có tag bị trùng.");

        // Template không có CatalogStatus — chỉ variant mới có.
    }

    private static bool HaveUniqueVariantCodes(List<CatalogVariantRequest>? variants)
    {
        if (variants == null) return true;
        return variants
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .Select(x => x.Code.Trim().ToUpperInvariant())
            .Distinct()
            .Count() == variants.Count(x => !string.IsNullOrWhiteSpace(x.Code));
    }

    private static bool HaveUniqueTagIds(List<CatalogTagRequest>? tags)
    {
        if (tags == null) return true;
        return tags.Select(x => x.ConceptTagId).Distinct().Count() == tags.Count;
    }

}

public class CreateCatalogProductCommandHandler : IRequestHandler<CreateCatalogProductCommand, DesignTemplateDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CreateCatalogProductCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignTemplateDTO> Handle(CreateCatalogProductCommand request, CancellationToken cancellationToken)
    {
        var now = CoreHelper.SystemTimeNow;
        var templateCode = request.Code.Trim();

        await ValidateReferencesAsync(request, cancellationToken);

        if (await _context.DesignTemplates.AnyAsync(x => x.Code == templateCode, cancellationToken))
        {
            throw new DuplicateException(nameof(DesignTemplate), nameof(request.Code), request.Code);
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var template = new DesignTemplate
        {
            Id = Guid.NewGuid(),
            Code = templateCode,
            Name = request.Name.Trim(),
            Description = request.Description,
            FileUrl = request.FileUrl.Trim(),
            ThumbnailUrl = request.ThumbnailUrl,
            Created = now,
            CreatedBy = _user.Username,
            LastModified = now,
            LastModifiedBy = _user.Username
        };

        foreach (var variantRequest in request.Variants)
        {
            var variantStatus = NormalizeCatalogStatus(variantRequest.CatalogStatus);
            template.Variants.Add(new DesignVariant
            {
                Id = Guid.NewGuid(),
                DesignTemplateId = template.Id,
                MaterialId = variantRequest.MaterialId,
                Code = variantRequest.Code.Trim(),
                Name = variantRequest.Name.Trim(),
                Description = variantRequest.Description,
                SizeScale = variantRequest.SizeScale,
                StockQuantity = variantRequest.StockQuantity,
                MinimumStockLevel = variantRequest.MinimumStockLevel,
                Price = variantRequest.Price,
                ImageUrls = variantRequest.ImageUrls?.Any() == true ? JsonSerializer.Serialize(variantRequest.ImageUrls) : null,
                PreviewModelUrl = variantRequest.PreviewModelUrl,
                IsAllowPreOrder = variantRequest.IsAllowPreOrder,
                EstimatedWeightPerUnit = variantRequest.EstimatedWeightPerUnit,
                EstimatedPrintTimePerUnit = variantRequest.EstimatedPrintTimePerUnit,
                MarkupPercentage = variantRequest.MarkupPercentage,
                CatalogStatus = variantStatus,
                IsActive = variantStatus == CatalogStatuses.Published,
                Created = now,
                CreatedBy = _user.Username,
                LastModified = now,
                LastModifiedBy = _user.Username
            });
        }

        foreach (var tagRequest in NormalizeTags(request.Tags))
        {
            template.DesignTags.Add(new DesignTag
            {
                Id = Guid.NewGuid(),
                DesignTemplateId = template.Id,
                ConceptTagId = tagRequest.ConceptTagId,
                IsMainTag = tagRequest.IsMainTag,
                IsActive = true,
                Created = now,
                CreatedBy = _user.Username,
                LastModified = now,
                LastModifiedBy = _user.Username
            });
        }

        _context.DesignTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        return await LoadTemplateDtoAsync(template.Id, cancellationToken);
    }

    private static string NormalizeCatalogStatus(string? status)
        => status?.ToUpperInvariant() ?? CatalogStatuses.Draft;

    private static IReadOnlyCollection<CatalogTagRequest> NormalizeTags(List<CatalogTagRequest>? tags)
    {
        if (tags == null || !tags.Any())
        {
            return [];
        }

        if (tags.Any(x => x.IsMainTag))
        {
            return tags;
        }

        return tags
            .Select((tag, index) => new CatalogTagRequest
            {
                ConceptTagId = tag.ConceptTagId,
                IsMainTag = index == 0
            })
            .ToList();
    }

    private async Task ValidateReferencesAsync(CreateCatalogProductCommand request, CancellationToken cancellationToken)
    {
        var variantCodes = request.Variants.Select(x => x.Code.Trim()).ToList();
        if (await _context.DesignVariants.AnyAsync(x => variantCodes.Contains(x.Code), cancellationToken))
        {
            throw new DuplicateException("Một hoặc nhiều mã biến thể đã tồn tại trong hệ thống.");
        }

        var materialIds = request.Variants.Select(x => x.MaterialId).Distinct().ToList();
        var validMaterialIds = await _context.Materials
            .Where(x => materialIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        if (materialIds.Except(validMaterialIds).Any())
        {
            throw new BusinessException("Một hoặc nhiều vật liệu không tồn tại hoặc đã ngưng hoạt động.");
        }

        var tagIds = (request.Tags ?? new List<CatalogTagRequest>()).Select(x => x.ConceptTagId).Distinct().ToList();
        var validTagIds = await _context.ConceptTags
            .Where(x => tagIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        if (tagIds.Except(validTagIds).Any())
        {
            throw new BusinessException("Một hoặc nhiều tag ý tưởng không tồn tại hoặc đã ngưng hoạt động.");
        }
    }

    private async Task<DesignTemplateDTO> LoadTemplateDtoAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var dto = await _context.DesignTemplates
            .AsNoTracking()
            .Include(x => x.Variants)
            .Include(x => x.DesignTags).ThenInclude(x => x.ConceptTag)
            .Where(x => x.Id == templateId)
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
        {
            throw new DataNotFoundException(nameof(DesignTemplate), templateId);
        }

        return _mapper.Map<DesignTemplateDTO>(dto);
    }
}
