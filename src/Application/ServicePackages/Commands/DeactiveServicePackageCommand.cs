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

public record DeactiveServicePackageCommand : IRequest<object>
{
    [Required]
    [JsonIgnore]
    public Guid Id { get; init; }

}
public class DeactiveServicePackageCommandHandler : IRequestHandler<DeactiveServicePackageCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeactiveServicePackageCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<object> Handle(DeactiveServicePackageCommand request, CancellationToken ct)
    {

        var service = await _context.ServicePackages.FindAsync(request.Id, ct);
        if (service == null)
        {
            throw new Exception("Không tìm thấy gói dịch vụ.");
        }

        service.IsActive = false;

        service.LastModified = CoreHelper.SystemTimeNow;
        service.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(ct);

        return request;
    }
}
