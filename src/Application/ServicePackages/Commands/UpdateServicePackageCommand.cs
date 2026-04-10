using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ServicePackages.Commands;

public record UpdateServicePackageCommand : IRequest<object>
{
    [Required]
    [JsonIgnore]
    public Guid Id { get; init; }
    public string? Code { get; init; }
    public string? Name { get; init; }
    public string? ServiceType { get; init; }
    public decimal? BasePrice { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }

}
public class UpdateServicePackageCommandValidator : AbstractValidator<UpdateServicePackageCommand>
{
    public UpdateServicePackageCommandValidator()
    {
        RuleFor(v => v.Code).NotEmpty().MaximumLength(10);
        RuleFor(v => v.Name).NotEmpty().MaximumLength(100);
        RuleFor(v => v.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(v => v.ServiceType).Must(x => x == "DESIGN" || x == "PRINTING")
            .WithMessage("ServiceType phải là DESIGN hoặc PRINTING.");
    }
}
public class UpdateServicePackageCommandHandler : IRequestHandler<UpdateServicePackageCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateServicePackageCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<object> Handle(UpdateServicePackageCommand request, CancellationToken ct)
    {
        // 1. Kiểm tra Code trùng lặp
        if (await _context.ServicePackages.AnyAsync(x => x.Code == request.Code, ct))
            throw new Exception($"Mã gói {request.Code} đã tồn tại.");

        var service = await _context.ServicePackages.FindAsync(request.Id, ct);
        if (service == null)
        {
            throw new Exception("Không tìm thấy gói dịch vụ.");
        }    

        service.Code = request.Code ?? service.Code;
        service.Name = request.Name ?? service.Name;
        service.ServiceType = request.ServiceType ?? service.ServiceType;
        service.BasePrice = request.BasePrice ?? service.BasePrice;
        service.Description = request.Description ?? service.Description;
        service.IsActive = request.IsActive ?? service.IsActive;

        service.LastModified = CoreHelper.SystemTimeNow;
        service.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(ct);

        return request;
    }
}
