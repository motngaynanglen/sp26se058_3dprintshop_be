using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Validation;
using sp26se058_3dprintshop_be.Application.Shipping;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
public record CreateShippingAddressCommand : IRequest<Guid>
{
    [Required]
    [DefaultValue("Nguyễn Văn A")]
    public string ReceiverName { get; set; } = null!;

    [Required]
    [DefaultValue("0901234567")]
    public string Phone { get; set; } = null!;

    [Required]
    [DefaultValue("123 Đường ABC, Phường 5")]
    public string AddressLine { get; set; } = null!;

    [DefaultValue("Phường Bến Nghé")]
    public string Ward { get; set; } = null!;

    [DefaultValue("Quận 1")]
    public string District { get; set; } = null!;

    [DefaultValue("Hồ Chí Minh")]
    public string City { get; set; } = null!;

    [DefaultValue("Việt Nam")]
    public string Province { get; set; } = "Việt Nam";

    [DefaultValue(false)]
    public bool IsDefault { get; set; } = false;

    public int? GhnDistrictId { get; set; }

    public string? GhnWardCode { get; set; }
}

public class CreateShippingAddressCommandValidator : AbstractValidator<CreateShippingAddressCommand>
{
    public CreateShippingAddressCommandValidator()
    {
        RuleFor(v => v.ReceiverName).NotEmpty().MaximumLength(255);
        RuleFor(v => v.Phone)
            .NotEmpty()
            .ValidVietnamesePhone();
        RuleFor(v => v.AddressLine).NotEmpty().MaximumLength(500);
    }
}

public class CreateShippingAddressHandler : IRequestHandler<CreateShippingAddressCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IGhnAddressResolver _ghnResolver;
    private readonly IUser _user;

    public CreateShippingAddressHandler(
        IApplicationDbContext context,
        IGhnAddressResolver ghnResolver,
        IUser user)
    {
        _context = context;
        _ghnResolver = ghnResolver;
        _user = user;
    }
    public async Task<Guid> Handle(CreateShippingAddressCommand request, CancellationToken cancellationToken)
    {
        Guid userId= _user.Id.ToGuid();
        var account = await _context.Accounts.Include(a => a.Customer).FirstOrDefaultAsync(a => a.Id == userId);
        if (account == null)
        {
            throw new Exception("Hãy đăng nhập!");
        }
        var customer = account.Customer;
        if (customer == null)
        {
            throw new Exception("Chỉ có khách hàng mới có thể tạo địa chỉ gửi hàng!");
        }

        // Nếu IsDefault = true, phải bỏ default của các địa chỉ cũ
        if (request.IsDefault)
        {
            var defaults = await _context.ShippingAddresses
                .Where(s => s.CustomerId == customer.Id && s.IsDefault)
                .ToListAsync();
            defaults.ForEach(x => x.IsDefault = false);
        }

        var entity = new ShippingAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            ReceiverName = request.ReceiverName,
            Phone = PhoneValidationExtensions.NormalizeVietnamesePhone(request.Phone),
            AddressLine = request.AddressLine,
            Ward = request.Ward,
            District = request.District,
            City = request.City,
            Province = request.Province,
            GhnDistrictId = request.GhnDistrictId,
            GhnWardCode = request.GhnWardCode,
            IsDefault = request.IsDefault,
        };

        await GhnAddressResolveHelper.EnsureGhnCodesAsync(entity, _ghnResolver, cancellationToken);

        _context.ShippingAddresses.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
