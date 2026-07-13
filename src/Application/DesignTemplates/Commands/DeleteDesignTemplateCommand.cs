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
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record DeleteDesignTemplateCommand : IRequest<bool>
{
    [JsonIgnore] // Ẩn khỏi JSON Body và Swagger
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid Id { get; init; }
}

public class DeleteDesignTemplateHander : IRequestHandler<DeleteDesignTemplateCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteDesignTemplateHander(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<bool> Handle(DeleteDesignTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _context.DesignTemplates
            .Include(t => t.Variants).ThenInclude(v => v.OrderItems)
            .FirstOrDefaultAsync(t=>t.Id == request.Id,cancellationToken);
        if (template == null)
        {
            throw new DataNotFoundException(nameof(DesignTemplates), request.Id);
        }
        bool hasOrders = template.Variants.Any(v => v.OrderItems.Any());

        if (hasOrders)
        {
            throw new DeleteFailureException(
                entityName: nameof(DesignTemplates),
                reason: "Mẫu này đã có trong danh sách đơn hàng của khách hàng, không thể xóa để đảm bảo lịch sử hệ thống."
            );
        }

        template.Deleted = CoreHelper.SystemTimeNow;
        template.DeletedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Nếu có lỗi lạ từ DB (ví dụ lỗi DB chết, lỗi kết nối)
            throw new DeleteFailureException(nameof(DesignTemplate), ex.Message);
        }

        return true;

    }
}
