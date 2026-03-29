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
        var variant = await _context.DesignVariants.FindAsync(new object[] { request.Id });
        if (variant == null)
        {
            throw new Exception("Không tìm thấy biến thể thiết kế");
        }
        variant.IsActive = false;
        variant.Deleted = DateTimeOffset.Now;
        variant.DeletedBy = userId;
        variant.LastModified = DateTime.Now;
        variant.LastModifiedBy = userId;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
