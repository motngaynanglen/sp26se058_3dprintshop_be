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

public record DeleteServicePackageCommand : IRequest<object>
{
    [Required]
    [JsonIgnore]
    public Guid Id { get; init; }

}
public class DeleteServicePackageCommandHandler : IRequestHandler<DeleteServicePackageCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteServicePackageCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<object> Handle(DeleteServicePackageCommand request, CancellationToken ct)
    {

        var service = await _context.ServicePackages.FindAsync(request.Id, ct);
        if (service == null)
        {
            throw new Exception("Không tìm thấy gói dịch vụ.");
        }

        service.Deleted = CoreHelper.SystemTimeNow;
        service.DeletedBy = _user.Username;

        await _context.SaveChangesAsync(ct);

        return request;
    }
}
