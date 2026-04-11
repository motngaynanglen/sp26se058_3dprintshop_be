using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

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
    public bool IsActive { get; init; } = true;
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
    private readonly IUser _user;

    public CreateServicePackageCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

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
            IsActive = request.IsActive,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
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
                MinQuantity = opt.IsRequired ? 1 : 0,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username,
                LastModified = CoreHelper.SystemTimeNow,
                LastModifiedBy = _user.Username,
            });
        }

        _context.ServicePackages.Add(entity);
        await _context.SaveChangesAsync(ct);

        return request;
    }
}
