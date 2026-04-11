using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.PackageOptions.Queries;
using sp26se058_3dprintshop_be.Application.ServicePackages.Commands;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.PackageOptions.Commands;
public record DeletePackageOptionCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid Id { get; set; }
}
public class DeletePackageOptionCommandHandler : IRequestHandler<DeletePackageOptionCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeletePackageOptionCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }
    public async Task<bool> Handle(DeletePackageOptionCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra cặp trùng lặp (Unique Constraint)
        var entity = await _context.PackageOptions.FindAsync(request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Exception("Option này đã tồn tại trong Package.");
        }

        entity.Deleted = CoreHelper.SystemTimeNow;
        entity.DeletedBy = _user.Username;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
