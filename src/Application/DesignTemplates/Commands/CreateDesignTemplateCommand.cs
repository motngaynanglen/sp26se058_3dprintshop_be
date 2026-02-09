using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;

public record CreateDesignTemplateCommand : IRequest<Guid>
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string FileUrl { get; init; } = null!;
    public string ThumbnailUrl { get; init; } = null!;
}

public class CreateDesignTemplateCommandHandler : IRequestHandler<CreateDesignTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateDesignTemplateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<Guid> Handle(CreateDesignTemplateCommand request, CancellationToken cancellationToken)
    {
        var newDesignTemplate = new Domain.Entities.DesignTemplate
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            FileUrl = request.FileUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            Created = DateTime.UtcNow
        };
        _context.DesignTemplates.Add(newDesignTemplate);
        await _context.SaveChangesAsync(cancellationToken);
        return newDesignTemplate.Id;
    }
}
