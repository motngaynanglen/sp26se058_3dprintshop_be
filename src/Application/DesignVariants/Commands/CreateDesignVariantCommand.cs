using System;
using System.ComponentModel;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record CreateDesignVariantCommand : IRequest<DesignVariantDTO>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid DesignTemplateId { get; init; }
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid MaterialId { get; init; }
    [DefaultValue("VAR-001")]
    public string Code { get; init; } = null!;
    [DefaultValue("Biến thể mặc định")]
    public string Name { get; init; } = null!;
    [DefaultValue("Biến thể mặc định với chất liệu PLA")]
    public string? Description { get; init; }
    [DefaultValue(1.0)]
    public decimal SizeScale { get; init; }
    [DefaultValue(100)]
    public int StockQuantity { get; init; }
    [DefaultValue(5)]
    public int? MinimumStockLevel { get; init; }
    [DefaultValue(50000.0)]
    public decimal Price { get; init; }
    public List<string>? ImageUrls { get; init; }
    [DefaultValue("https://example.com/preview-model.stl")]
    public string? PreviewModelUrl { get; init; }
    [DefaultValue(true)]
    public bool IsAllowPreOrder { get; init; }
    [DefaultValue(0.5)]
    public decimal EstimatedWeightPerUnit { get; init; }
    [DefaultValue(120.0)]
    public decimal EstimatedPrintTimePerUnit { get; init; }
    [DefaultValue(0)]
    public decimal MarkupPercentage { get; init; }
    [DefaultValue(CatalogStatuses.Draft)]
    public string? CatalogStatus { get; init; }
    [DefaultValue(null)]
    public bool? IsActive { get; init; }
}

public class CreateDesignVariantCommandValidator : AbstractValidator<CreateDesignVariantCommand>
{
    public CreateDesignVariantCommandValidator()
    {
        RuleFor(x => x.DesignTemplateId)
            .NotEmpty().WithMessage("Mẫu thiết kế không hợp lệ.");

        RuleFor(x => x.MaterialId)
            .NotEmpty().WithMessage("Vật liệu không hợp lệ.");

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
            .GreaterThan(0).WithMessage("Tỉ lệ kích thước phải lớn hơn 0.");

        RuleFor(x => x.EstimatedWeightPerUnit)
            .GreaterThan(0).WithMessage("Khối lượng ước tính phải lớn hơn 0.");

        RuleFor(x => x.EstimatedPrintTimePerUnit)
            .GreaterThan(0).WithMessage("Thời gian in ước tính phải lớn hơn 0.");

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

public class CreateDesignVariantCommandHandler : IRequestHandler<CreateDesignVariantCommand, DesignVariantDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;       
    private readonly IUser _user;

    public CreateDesignVariantCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignVariantDTO> Handle(CreateDesignVariantCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra DesignTemplate tồn tại (Sử dụng NotFoundException - DB_004)
        var templateExists = await _context.DesignTemplates
            .AnyAsync(t => t.Id == request.DesignTemplateId, cancellationToken);

        if (!templateExists)
            throw new DataNotFoundException(nameof(DesignTemplate), request.DesignTemplateId);
        // 2. Kiểm tra Material tồn tại (Sử dụng DataNotFoundException - DB_004)
        var materialExists = await _context.Materials
            .AnyAsync(m => m.Id == request.MaterialId, cancellationToken);

        if (!materialExists)
            throw new DataNotFoundException(nameof(Material), request.MaterialId);
        // 3. Kiểm tra trùng lặp Code của Variant (Sử dụng DuplicateException - DB_001)
        var code = request.Code.Trim();
        var codeExists = await _context.DesignVariants
            .AnyAsync(dv => dv.Code == code, cancellationToken);

        if (codeExists)
            throw new DuplicateException(nameof(DesignVariant), nameof(request.Code), request.Code);

        var catalogStatus = request.CatalogStatus?.ToUpperInvariant()
            ?? (request.IsActive == true ? CatalogStatuses.Published : CatalogStatuses.Draft);

        var newDesignVariant = new DesignVariant
        {
            DesignTemplateId = request.DesignTemplateId,
            MaterialId = request.MaterialId,
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description,
            SizeScale = request.SizeScale,
            StockQuantity = request.StockQuantity,
            MinimumStockLevel = request.MinimumStockLevel,
            Price = request.Price,
            ImageUrls = request.ImageUrls?.Any() == true ? JsonSerializer.Serialize(request.ImageUrls) : null,
            PreviewModelUrl = request.PreviewModelUrl,
            IsAllowPreOrder = request.IsAllowPreOrder,
            EstimatedWeightPerUnit = request.EstimatedWeightPerUnit,
            EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit,
            MarkupPercentage = request.MarkupPercentage,
            CatalogStatus = catalogStatus,
            IsActive = catalogStatus == CatalogStatuses.Published,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };
        _context.DesignVariants.Add(newDesignVariant);

        // Tạo InventoryTransaction nếu có nhập tồn kho ban đầu
        if (request.StockQuantity > 0)
        {
            var staffId = await _context.Staffs
                .Where(s => s.AccountId == Guid.Parse(_user.Id!))
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                DesignVariantId = newDesignVariant.Id,
                StaffId = staffId != Guid.Empty ? staffId : null,
                Type = InventoryTransactionTypes.PurchaseIn,
                Quantity = request.StockQuantity,
                Note = $"Nhập kho lần đầu khi tạo biến thể {code}",
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username,
                LastModified = CoreHelper.SystemTimeNow,
                LastModifiedBy = _user.Username,
            });
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new CreateFailureException(nameof(DesignVariant), ex.Message);
        }
        return _mapper.Map<DesignVariantDTO>(newDesignVariant);
    }
}
