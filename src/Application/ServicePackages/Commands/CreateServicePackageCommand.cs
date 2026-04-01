using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ServicePackages.Commands;
public record CreatePackageOptionDTO(
    Guid ServiceOptionId,
    bool IsRequired,
    decimal? PriceOverride,
    int MaxQuantity
);
public record CreateServicePackageCommand : IRequest<object>
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string ServiceType { get; init; } = null!;
    public decimal BasePrice { get; init; }
    public string? Description { get; init; }

    // Danh sách các Option gán vào gói ngay khi tạo
    public List<CreatePackageOptionDTO> Options { get; init; } = new();
}
public class CreateServicePackageCommandValidator : AbstractValidator<CreateServicePackageCommand>
{
    public CreateServicePackageCommandValidator()
    {
        RuleFor(v => v.Code).NotEmpty().MaximumLength(10);
        RuleFor(v => v.Name).NotEmpty().MaximumLength(100);
        RuleFor(v => v.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(v => v.ServiceType).Must(x => x == "DESIGN" || x == "PRINTING")
            .WithMessage("ServiceType phải là DESIGN hoặc PRINTING.");
    }
}
public class CreateServicePackageCommandHandler : IRequestHandler<CreateServicePackageCommand, object>
{
    private readonly IApplicationDbContext _context;

    public CreateServicePackageCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<object> Handle(CreateServicePackageCommand request, CancellationToken ct)
    {
        // 1. Kiểm tra Code trùng lặp
        if (await _context.ServicePackages.AnyAsync(x => x.Code == request.Code, ct))
            throw new Exception($"Mã gói {request.Code} đã tồn tại.");

        var entity = new ServicePackage
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            ServiceType = request.ServiceType,
            BasePrice = request.BasePrice,
            Description = request.Description,
            IsActive = true
        };

        // 2. Thêm các Option vào gói
        foreach (var opt in request.Options)
        {
            entity.PackageOptions.Add(new PackageOption
            {
                Id = Guid.NewGuid(),
                ServiceOptionId = opt.ServiceOptionId,
                IsRequired = opt.IsRequired,
                PriceOverride = opt.PriceOverride,
                MaxQuantity = opt.MaxQuantity,
                MinQuantity = opt.IsRequired ? 1 : 0
            });
        }

        _context.ServicePackages.Add(entity);
        await _context.SaveChangesAsync(ct);

        return request;
    }
}
