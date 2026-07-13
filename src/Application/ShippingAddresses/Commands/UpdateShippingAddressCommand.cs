using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;
using sp26se058_3dprintshop_be.Application.ShippingAddresses.Queries;

namespace sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
[Authorize(Roles = Roles.CUSTOMER)]
public record UpdateShippingAddressCommand : IRequest<ShippingAddressDTO>
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
    [DefaultValue("123 Đường ABC")]
    public string? AddressLine { get; set; }

    [DefaultValue("Phường Bến Nghé")]
    public string? Ward { get; set; }

    [DefaultValue("Quận 1")]
    public string? District { get; set; }

    [DefaultValue("Hồ Chí Minh")]
    public string? City { get; set; }

    [DefaultValue("Việt Nam")]
    public string? Province { get; set; }

    public int? GhnDistrictId { get; set; }

    public string? GhnWardCode { get; set; }

    [DefaultValue(false)]
    public bool? IsDefault { get; set; }
}
public class UpdateShippingAddressCommandHandler : IRequestHandler<UpdateShippingAddressCommand, ShippingAddressDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;
    public UpdateShippingAddressCommandHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }
    public async Task<ShippingAddressDTO> Handle(UpdateShippingAddressCommand request, CancellationToken cancellationToken)
    {
        Guid userId = _user.Id.ToGuid();
        var entity = await _context.ShippingAddresses
            .Include(x => x.Customer) // Load Customer để check AccountId
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        // BÁO LỖI NOT FOUND: Không tồn tại Id này trong hệ thống
        if (entity == null)
        {
            throw new DataNotFoundException(nameof(ShippingAddress), request.Id);
        }
        // BÁO LỖI FORBIDDEN: Có tồn tại địa chỉ, nhưng nó không thuộc về user đang đăng nhập
        if (entity.Customer.AccountId != userId)
        {
            throw new ForbiddenAccessException("Bạn không có quyền chỉnh sửa địa chỉ giao hàng này.");
        }
        if (request.IsDefault == true && !entity.IsDefault)
        {
            var existingDefaults = await _context.ShippingAddresses
            .Where(s => s.CustomerId == entity.CustomerId && s.IsDefault)
            .ToListAsync(cancellationToken);

            foreach (var addr in existingDefaults)
            {
                addr.IsDefault = false;
            }
            entity.IsDefault = true;
        }
        entity.ReceiverName = request.ReceiverName ?? entity.ReceiverName;
        entity.Phone = request.Phone ?? entity.Phone;
        entity.AddressLine = request.AddressLine ?? entity.AddressLine;
        entity.Ward = request.Ward ?? entity.Ward;
        entity.District = request.District ?? entity.District;
        entity.City = request.City ?? entity.City;
        entity.Province = request.Province ?? entity.Province;
        entity.GhnDistrictId = request.GhnDistrictId ?? entity.GhnDistrictId;
        entity.GhnWardCode = request.GhnWardCode?.Trim() ?? entity.GhnWardCode;

        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException(nameof(ShippingAddress), $"{ex.InnerException?.Message ?? ex.Message}");
        }
        return _mapper.Map<ShippingAddressDTO>(entity);
    }
}
