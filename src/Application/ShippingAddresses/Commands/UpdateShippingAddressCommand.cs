using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
public record UpdateShippingAddressCommand : IRequest<Guid>
{
    [Required]
    [JsonIgnore]
    public Guid Id { get; set; }
    [Required]
    [DefaultValue("Nguyễn Văn A")]
    public string? ReceiverName { get; set; }

    [Required]
    [DefaultValue("0901234567")]
    public string? Phone { get; set; }

    [Required]
    [DefaultValue("123 Đường ABC, Phường 5")]
    public string? AddressLine { get; set; }

    [DefaultValue("Phường Bến Nghé")]
    public string? Ward { get; set; }

    [DefaultValue("Quận 1")]
    public string? District { get; set; }

    [DefaultValue("Hồ Chí Minh")]
    public string? City { get; set; }

    [DefaultValue("Việt Nam")]
    public string? Province { get; set; }
    [DefaultValue(true)]
    public bool? IsDefault { get; set; }
}
public class UpdateShippingAddressCommandHandler : IRequestHandler<UpdateShippingAddressCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    public UpdateShippingAddressCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }
    public async Task<Guid> Handle(UpdateShippingAddressCommand request, CancellationToken cancellationToken)
    {
        Guid userId = _user.Id.ToGuid();
        var entity = await _context.ShippingAddresses
              .FirstOrDefaultAsync(x => x.Id == request.Id && x.Customer.AccountId == userId, cancellationToken);

        if (entity == null) throw new Exception("Không tìm thấy địa chỉ hoặc bạn không có quyền sửa!");

        if (request.IsDefault == true && !entity.IsDefault)
        {
            var existingDefaults = await _context.ShippingAddresses
                .Where(s => s.CustomerId == entity.CustomerId && s.IsDefault)
                .ToListAsync(cancellationToken);
            existingDefaults.ForEach(x => x.IsDefault = false);
            entity.IsDefault = true;
        }
        entity.ReceiverName = request.ReceiverName ?? entity.ReceiverName;
        entity.Phone = request.Phone ?? entity.Phone;
        entity.AddressLine = request.AddressLine ?? entity.AddressLine;
        entity.Ward = request.Ward ?? entity.Ward;
        entity.District = request.District ?? entity.District;
        entity.City = request.City ?? entity.City;
        entity.Province = request.Province ?? entity.Province;
        
        entity.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
