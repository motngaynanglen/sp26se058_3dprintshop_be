using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
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
    [DefaultValue("Biến thể mặc định 2 với chất liệu PLA")]
    public string? Description { get; init; }
    [DefaultValue(1.0)]
    public decimal? SizeScale { get; init; }
    [DefaultValue(100)]
    public int? StockQuantity { get; init; }
    [DefaultValue(5)]
    public int? MinimumStockLevel { get; init; }
    [DefaultValue(50000.0)]
    public decimal? Price { get; init; }
    public List<string>? ImageUrls { get; init; }
    [DefaultValue(false)]
    public bool ClearImageUrls { get; init; }
    [DefaultValue("https://example.com/preview-model.stl")]
    public string? PreviewModelUrl { get; init; }
    [DefaultValue(false)]
    public bool? IsAllowPreOrder { get; init; }
    [DefaultValue(0.5)]
    public decimal? EstimatedWeightPerUnit { get; init; }
    [DefaultValue(120.0)]
    public decimal? EstimatedPrintTimePerUnit { get; init; }
    [DefaultValue(0)]
    public decimal? MarkupPercentage { get; init; }
    [DefaultValue(CatalogStatuses.Published)]
    public string? CatalogStatus { get; init; }
    [DefaultValue(null)]
    public bool? IsActive { get; init; }
}

public class UpdateDesignVariantCommandValidator : AbstractValidator<UpdateDesignVariantCommand>
{
    public UpdateDesignVariantCommandValidator()
    {
        RuleFor(x => x.Code)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("Mã biến thể không được để trống.");

        RuleFor(x => x.Name)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("Tên biến thể không được để trống.");

        RuleFor(x => x.Price)
            .GreaterThan(0).When(x => x.Price.HasValue)
            .WithMessage("Giá biến thể phải lớn hơn 0.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).When(x => x.StockQuantity.HasValue)
            .WithMessage("Số lượng tồn kho không được âm.");

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
            .GreaterThanOrEqualTo(0).When(x => x.MarkupPercentage.HasValue)
            .WithMessage("Phần trăm phụ thu không được âm.");

        RuleFor(x => x.CatalogStatus)
            .Must(x => string.IsNullOrWhiteSpace(x) || CatalogStatuses.IsValid(x))
            .WithMessage("Trạng thái catalog không hợp lệ.");

        RuleFor(x => x.ImageUrls)
            .Must(x => x == null || x.All(url => !string.IsNullOrWhiteSpace(url)))
            .WithMessage("Đường dẫn ảnh biến thể không được để trống.");
    }
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
        if (!string.IsNullOrEmpty(request.Code) && request.Code.Trim() != entity.Code)
        {
            var code = request.Code.Trim();
            var codeExists = await _context.DesignVariants
                .AnyAsync(dv => dv.Code == code && dv.Id != request.Id, cancellationToken);

            if (codeExists)
                throw new DuplicateException(nameof(DesignVariant), nameof(request.Code), request.Code);

            entity.Code = code;
        }

        // Update only fields that are provided (nullable)
        if (request.Name != null) entity.Name = request.Name.Trim();
        if (request.SizeScale.HasValue) entity.SizeScale = request.SizeScale.Value;
        if (request.StockQuantity.HasValue) entity.StockQuantity = request.StockQuantity.Value;
        if (request.MinimumStockLevel.HasValue) entity.MinimumStockLevel = request.MinimumStockLevel.Value;
        if (request.Price.HasValue) entity.Price = request.Price.Value;
        if (request.ClearImageUrls) entity.ImageUrls = null;
        else if (request.ImageUrls != null) entity.ImageUrls = request.ImageUrls.Any() ? JsonSerializer.Serialize(request.ImageUrls) : null;
        if (request.IsAllowPreOrder.HasValue) entity.IsAllowPreOrder = request.IsAllowPreOrder.Value;
        if (request.EstimatedWeightPerUnit.HasValue) entity.EstimatedWeightPerUnit = request.EstimatedWeightPerUnit.Value;
        if (request.EstimatedPrintTimePerUnit.HasValue) entity.EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit.Value;
        if (request.MarkupPercentage.HasValue) entity.MarkupPercentage = request.MarkupPercentage.Value;
        if (!string.IsNullOrWhiteSpace(request.CatalogStatus))
        {
            var catalogStatus = request.CatalogStatus.ToUpperInvariant();

            entity.CatalogStatus = catalogStatus;
            entity.IsActive = catalogStatus == CatalogStatuses.Published;
        }
        else if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
            entity.CatalogStatus = request.IsActive.Value ? CatalogStatuses.Published : CatalogStatuses.Draft;
        }
        if(request.Description != null) entity.Description = request.Description;
        if(request.PreviewModelUrl != null) entity.PreviewModelUrl = request.PreviewModelUrl;

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
