using System;
using MediatR;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination; // ← Thêm using này

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;

public record CreateDesignTemplateCommand : IRequest<DesignTemplateDTO>   // ← Thay Guid bằng DesignTemplateDTO
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string FileUrl { get; init; } = null!;
    public string ThumbnailUrl { get; init; } = null!;
}

public class CreateDesignTemplateCommandHandler : IRequestHandler<CreateDesignTemplateCommand, DesignTemplateDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;                    // ← Thêm IMapper

    public CreateDesignTemplateCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<DesignTemplateDTO> Handle(CreateDesignTemplateCommand request, CancellationToken cancellationToken)
    {
        var newDesignTemplate = new Domain.Entities.DesignTemplate
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            FileUrl = request.FileUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            IsActive = false,
            Created = DateTime.UtcNow
        };

        _context.DesignTemplates.Add(newDesignTemplate);
        await _context.SaveChangesAsync(cancellationToken);

        // Map sang DTO và trả về
        var designTemplateDto = _mapper.Map<DesignTemplateDTO>(newDesignTemplate);

        return designTemplateDto;
    }
}
