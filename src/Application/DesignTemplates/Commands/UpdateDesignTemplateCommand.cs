using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;

public record UpdateDesignTemplateCommand : IRequest<Guid>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    // Dữ liệu cần update
    public string? Code { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? FileUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
}

public class UpdateDesignTemplateCommandHandler : IRequestHandler<UpdateDesignTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public UpdateDesignTemplateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<Guid> Handle(UpdateDesignTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.DesignTemplates
              .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity == null) throw new Exception("Không tìm thấy mẫu thiết kế");
        if (!string.IsNullOrEmpty(request.Code)) entity.Code = request.Code;
        if (!string.IsNullOrEmpty(request.Name)) entity.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Description)) entity.Description = request.Description;
        if (!string.IsNullOrEmpty(request.FileUrl)) entity.FileUrl = request.FileUrl;
        if (!string.IsNullOrEmpty(request.ThumbnailUrl)) entity.ThumbnailUrl = request.ThumbnailUrl;
        entity.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
