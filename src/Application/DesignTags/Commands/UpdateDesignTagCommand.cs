using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.DesignTags.Commands;
public record UpdateDesignTagCommand : IRequest<UpdateDesignTagCommand>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    [JsonIgnore]
    public Guid Id { get; set; }
    [DefaultValue(false)]
    public bool IsMainTag { get; set; }
}
public class UpdateDesignTagCommandHandler : IRequestHandler<UpdateDesignTagCommand, UpdateDesignTagCommand>
{
    private readonly IApplicationDbContext _context;
    public UpdateDesignTagCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<UpdateDesignTagCommand> Handle(UpdateDesignTagCommand request, CancellationToken ct)
    {
        var entity = await _context.DesignTags.FindAsync(request.Id);
        if (entity == null) throw new Exception("Tag không tồn tại.");

        if (request.IsMainTag)
        {
            // Tắt các MainTag khác của cùng Template đó
            var others = await _context.DesignTags
                .Where(x => x.DesignTemplateId == entity.DesignTemplateId && x.Id != entity.Id)
                .ToListAsync(ct);
            foreach (var tag in others) tag.IsMainTag = false;
        }

        entity.IsMainTag = request.IsMainTag;
        await _context.SaveChangesAsync(ct);
        return request;
    }
}
