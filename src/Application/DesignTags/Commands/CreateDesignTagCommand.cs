using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.DesignTags.Commands;
public record CreateDesignTagCommand : IRequest<CreateDesignTagCommand>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid DesignTemplateId { get; set; }
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid ConceptTagId { get; set; }
    [DefaultValue(false)]
    public bool IsMainTag { get; set; }
}
public class CreateDesignTagCommandHandler : IRequestHandler<CreateDesignTagCommand, CreateDesignTagCommand>
{
    private readonly IApplicationDbContext _context;
    public CreateDesignTagCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<CreateDesignTagCommand> Handle(CreateDesignTagCommand request, CancellationToken ct)
    {
        // Kiểm tra nếu set là Main, phải bỏ Main cũ
        if (request.IsMainTag)
        {
            var oldMain = await _context.DesignTags.FirstOrDefaultAsync(x => x.DesignTemplateId == request.DesignTemplateId && x.IsMainTag, ct);
            if (oldMain != null) oldMain.IsMainTag = false;
        }

        var entity = new DesignTag
        {
            Id = Guid.NewGuid(),
            DesignTemplateId = request.DesignTemplateId,
            ConceptTagId = request.ConceptTagId,
            IsMainTag = request.IsMainTag
        };

        _context.DesignTags.Add(entity);
        await _context.SaveChangesAsync(ct);
        return request;
    }
}
