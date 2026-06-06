using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Validation;
using sp26se058_3dprintshop_be.Application.Shipping;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;

public record UpdateShippingAddressCommand : IRequest<Guid>
{
    [JsonIgnore]
    public Guid Id { get; init; }

    public string? ReceiverName { get; init; }
    public string? Phone { get; init; }
    public string? AddressLine { get; init; }
    public string? Ward { get; init; }
    public string? District { get; init; }
    public string? City { get; init; }

    [DefaultValue("Việt Nam")]
    public string? Province { get; init; }

    public int? GhnDistrictId { get; init; }
    public string? GhnWardCode { get; init; }

    [DefaultValue(true)]
    public bool? IsDefault { get; init; }
}

public class UpdateShippingAddressCommandValidator : AbstractValidator<UpdateShippingAddressCommand>
{
    public UpdateShippingAddressCommandValidator()
    {
        RuleFor(v => v.ReceiverName).MaximumLength(255).When(v => !string.IsNullOrWhiteSpace(v.ReceiverName));
        RuleFor(v => v.Phone)
            .ValidVietnamesePhone()
            .When(v => !string.IsNullOrWhiteSpace(v.Phone));
        RuleFor(v => v.AddressLine).MaximumLength(500).When(v => !string.IsNullOrWhiteSpace(v.AddressLine));
    }
}

public class UpdateShippingAddressCommandHandler : IRequestHandler<UpdateShippingAddressCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IGhnAddressResolver _ghnResolver;
    private readonly IUser _user;

    public UpdateShippingAddressCommandHandler(
        IApplicationDbContext context,
        IGhnAddressResolver ghnResolver,
        IUser user)
    {
        _context = context;
        _ghnResolver = ghnResolver;
        _user = user;
    }

    public async Task<Guid> Handle(UpdateShippingAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid();
        var entity = await _context.ShippingAddresses
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.Customer.AccountId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy địa chỉ.");

        if (request.IsDefault == true)
        {
            var defaults = await _context.ShippingAddresses
                .Where(s => s.CustomerId == entity.CustomerId && s.IsDefault && s.Id != entity.Id)
                .ToListAsync(cancellationToken);
            defaults.ForEach(x => x.IsDefault = false);
        }

        if (!string.IsNullOrWhiteSpace(request.ReceiverName))
            entity.ReceiverName = request.ReceiverName;
        if (!string.IsNullOrWhiteSpace(request.Phone))
            entity.Phone = PhoneValidationExtensions.NormalizeVietnamesePhone(request.Phone);
        if (!string.IsNullOrWhiteSpace(request.AddressLine))
            entity.AddressLine = request.AddressLine;
        if (!string.IsNullOrWhiteSpace(request.Ward))
            entity.Ward = request.Ward;
        if (!string.IsNullOrWhiteSpace(request.District))
            entity.District = request.District;
        if (!string.IsNullOrWhiteSpace(request.City))
            entity.City = request.City;
        entity.Province = request.Province ?? entity.Province;
        if (request.GhnDistrictId is > 0)
            entity.GhnDistrictId = request.GhnDistrictId;
        if (!string.IsNullOrWhiteSpace(request.GhnWardCode))
            entity.GhnWardCode = request.GhnWardCode.Trim();
        if (request.IsDefault.HasValue)
            entity.IsDefault = request.IsDefault.Value;

        await GhnAddressResolveHelper.EnsureGhnCodesAsync(entity, _ghnResolver, cancellationToken);

        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
