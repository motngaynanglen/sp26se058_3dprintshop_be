using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
public record CreateShippingAddressCommand : IRequest<CreateShippingAddressCommand>
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
public class CreateShippingAddressHandler : IRequestHandler<CreateShippingAddressCommand, CreateShippingAddressCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    public CreateShippingAddressHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }
    public async Task<CreateShippingAddressCommand> Handle(CreateShippingAddressCommand request, CancellationToken cancellationToken)
    {
        Guid userId= _user.Id.ToGuid();
        // Nếu IsDefault = true, phải bỏ default của các địa chỉ cũ
        if (request.IsDefault)
        {
            var defaults = await _context.ShippingAddresses
                .Where(s => s.CustomerId == userId && s.IsDefault)
                .ToListAsync();
            defaults.ForEach(x => x.IsDefault = false);
        }
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
        await _context.SaveChangesAsync(cancellationToken);
        return request;
    }
}
