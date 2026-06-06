using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Commands;

public record DeleteDesignVariantCommand : IRequest<bool>
{
    [JsonIgnore] // Ẩn khỏi JSON Body và Swagger
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; init; }
}

public class DeleteDesignVariantCommandHandler : IRequestHandler<DeleteDesignVariantCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    public DeleteDesignVariantCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context; 
        _user = user;
    }
    public async Task<bool> Handle(DeleteDesignVariantCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id;
        var variant = await _context.DesignVariants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);
        if (variant == null)
        {
            throw new Exception("Không tìm thấy biến thể thiết kế");
        }

        variant.IsActive = !variant.IsActive;

        if (variant.IsActive)
        {
            variant.Deleted = null;
            variant.DeletedBy = null;
        }

        variant.LastModified = DateTimeOffset.UtcNow;
        variant.LastModifiedBy = userId;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
