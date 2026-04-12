using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record DeleteDesignVariantCommand : IRequest<bool>
{
    [JsonIgnore] // Ẩn khỏi JSON Body và Swagger
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
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
        var variant = await _context.DesignVariants
             .Include(v => v.OrderItems)
             .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);
        if (variant == null)
        {
            throw new DataNotFoundException(nameof(DesignVariant), request.Id);
        }
        if (variant.OrderItems.Any())
        {
            throw new DeleteFailureException(
                entityName: nameof(DesignVariant),
                reason: "Biến thể này đã có trong lịch sử đơn hàng, không thể xóa để đảm bảo báo cáo thống kê."
            );
        }
        variant.IsActive = false;
        variant.Deleted = CoreHelper.SystemTimeNow;
        variant.DeletedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DeleteFailureException(nameof(DesignVariant), ex.Message);
        }

        return true;
    }
}
