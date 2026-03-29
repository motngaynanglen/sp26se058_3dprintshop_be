using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.DesignTags.Commands;
public record DeleteDesignTagCommand : IRequest<bool>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid Id { get; set; }

}
public class DeleteDesignTagCommandHandler : IRequestHandler<DeleteDesignTagCommand, bool>
{
    private readonly IApplicationDbContext _context;
    public DeleteDesignTagCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteDesignTagCommand request, CancellationToken ct)
    {
        var entity = await _context.DesignTags.FindAsync(request.Id);
        if (entity == null) return false;

        bool wasMain = entity.IsMainTag;
        var templateId = entity.DesignTemplateId;

        _context.DesignTags.Remove(entity);

        // Safety Check: Nếu xóa mất MainTag, đẩy thằng còn lại lên làm Main
        if (wasMain)
        {
            var nextTag = await _context.DesignTags
                .Where(x => x.DesignTemplateId == templateId && x.Id != request.Id)
                .FirstOrDefaultAsync(ct);

            if (nextTag != null) nextTag.IsMainTag = true;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}
}
