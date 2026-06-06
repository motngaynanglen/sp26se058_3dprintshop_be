using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;

public record DeleteDesignTemplateCommand : IRequest<bool>
{
    [JsonIgnore] // Ẩn khỏi JSON Body và Swagger
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
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
        var userId = _user.Id;
        var template = await _context.DesignTemplates.FindAsync(new object[] { request.Id });
        if (template == null)
        {
            throw new Exception("Không tìm thấy Thiết kế");
        }

        template.IsActive = false;
        template.Deleted = DateTime.Now;
        template.DeletedBy = userId;
        template.LastModified = DateTime.Now;
        template.LastModifiedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;

    }
}
