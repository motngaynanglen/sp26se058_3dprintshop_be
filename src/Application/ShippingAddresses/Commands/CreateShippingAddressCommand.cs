using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Application.ShippingAddresses.Queries;

namespace sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
[Authorize(Roles = Roles.CUSTOMER)]
public record CreateShippingAddressCommand : IRequest<ShippingAddressDTO>
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
}
public class CreateShippingAddressHandler : IRequestHandler<CreateShippingAddressCommand, ShippingAddressDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;
    public CreateShippingAddressHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }
    public async Task<ShippingAddressDTO> Handle(CreateShippingAddressCommand request, CancellationToken cancellationToken)
    {
        Guid userId= _user.Id.ToGuid();
        var account = await _context.Accounts.Include(a => a.Customer).FirstOrDefaultAsync(a => a.Id == userId);
        if (account == null)
        {
            throw new UnauthorizedAccessException("Hãy đăng nhập!");
        }
        var customer = account.Customer;
        if (customer == null)
        {
            throw new ForbiddenAccessException("Chỉ có khách hàng mới có thể tạo địa chỉ gửi hàng!");
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
            Phone = request.Phone,
            AddressLine = request.AddressLine,
            Ward = request.Ward,
            District = request.District,
            City = request.City,
            Province = request.Province,
            IsDefault = request.IsDefault,
        };

        _context.ShippingAddresses.Add(entity);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new CreateFailureException(nameof(ShippingAddress), $"{ex.InnerException?.Message ?? ex.Message}");
        }
        return _mapper.Map<ShippingAddressDTO>(entity);
    }
}
