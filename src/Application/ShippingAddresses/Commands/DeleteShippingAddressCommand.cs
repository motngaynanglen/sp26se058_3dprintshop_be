using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
public record DeleteShippingAddressCommand : IRequest<bool>
{
    [Required]
    [JsonIgnore]
    public Guid Id { get; set; }
}
public class DeleteShippingAddressHandler : IRequestHandler<DeleteShippingAddressCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteShippingAddressHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<bool> Handle(DeleteShippingAddressCommand request, CancellationToken cancellationToken)
    {
        Guid userId = _user.Id.ToGuid();
        var entity = await _context.ShippingAddresses
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.Customer.AccountId == userId, cancellationToken);

        if (entity == null) throw new Exception("Không tìm thấy địa chỉ để xóa!");

        var wasDefault = entity.IsDefault;
        entity.Deleted = CoreHelper.SystemTimeNow;
        entity.DeletedBy = _user.Username;
        entity.IsDefault = false;

        if (wasDefault)
        {
            var nextDefault = await _context.ShippingAddresses
                .Where(x => x.CustomerId == entity.CustomerId && x.Id != entity.Id)
                .OrderByDescending(x => x.Created)
                .FirstOrDefaultAsync(cancellationToken);
            if (nextDefault != null)
                nextDefault.IsDefault = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
