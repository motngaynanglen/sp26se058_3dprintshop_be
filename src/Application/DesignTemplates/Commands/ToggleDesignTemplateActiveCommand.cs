using System.ComponentModel;
using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;

public record ToggleDesignTemplateActiveCommand : IRequest<DesignTemplateDTO>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; init; }
}

public class ToggleDesignTemplateActiveCommandHandler
    : IRequestHandler<ToggleDesignTemplateActiveCommand, DesignTemplateDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public ToggleDesignTemplateActiveCommandHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignTemplateDTO> Handle(
        ToggleDesignTemplateActiveCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _context.DesignTemplates
            .FirstOrDefaultAsync(dt => dt.Id == request.Id, cancellationToken);

        if (template == null)
            throw new Exception($"DesignTemplate with id {request.Id} not found.");

        template.IsActive = !template.IsActive;
        template.LastModified = DateTimeOffset.UtcNow;
        template.LastModifiedBy = _user.Id;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DesignTemplateDTO>(template);
    }
}
