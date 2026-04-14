using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignLogs.Commands;

public record CreateDesignLogCommand : IRequest<DesignLogDTO>
{
    //[DefaultValue(000)]
    public Guid DesignWorkId { get; init; }
    [DefaultValue("Nội dung log")]
    public string? Content { get; init; }
    [DefaultValue("[\"https://f005.backblazeb2.com/file/3dprintshop/models/62b1ae312d6a48b69972d17e8058fe4c.glb\"]")]
    public List<string>? ImageUrls { get; init; }
    [DefaultValue("COMMUNICATION hoặc INTERNAL_NOTE")]
    public string LogType { get; init; } = "COMMUNICATION";
}

public class CreateDesignLogCommandHandler : IRequestHandler<CreateDesignLogCommand, DesignLogDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CreateDesignLogCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignLogDTO> Handle(CreateDesignLogCommand request, CancellationToken cancellationToken)
    {
        var log = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = request.DesignWorkId,
            AccountId = _user.Id != null ? Guid.Parse(_user.Id) : null,
            Content = request.Content,
            LogType = request.LogType,
            IsAI = false,
            Metadata = request.ImageUrls != null ? JsonSerializer.Serialize(request.ImageUrls) : null,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username
        };

        _context.DesignLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DesignLogDTO>(log);
    }
}
