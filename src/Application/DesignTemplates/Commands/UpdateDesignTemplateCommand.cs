using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Exceptions; // Nếu bạn có NotFoundException
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;

public record UpdateDesignTemplateCommand : IRequest<DesignTemplateDTO>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; init; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public string ThumbnailUrl { get; set; } = null!;
}

public class UpdateDesignTemplateCommandHandler : IRequestHandler<UpdateDesignTemplateCommand, DesignTemplateDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateDesignTemplateCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<DesignTemplateDTO> Handle(UpdateDesignTemplateCommand request, CancellationToken cancellationToken)
    {
        var designTemplate = await _context.DesignTemplates
            .FirstOrDefaultAsync(dt => dt.Id == request.Id, cancellationToken);

        if (designTemplate == null)
        {
            throw new Exception($"DesignTemplate with id {request.Id} not found.");
        }

        // Cập nhật thông tin
        designTemplate.Code = request.Code;
        designTemplate.Name = request.Name;
        designTemplate.Description = request.Description;
        designTemplate.FileUrl = request.FileUrl;
        designTemplate.ThumbnailUrl = request.ThumbnailUrl;
        // designTemplate.LastModified = DateTime.UtcNow;   // Nếu có trường này thì mở ra

        await _context.SaveChangesAsync(cancellationToken);

        // Map sang DTO
        var designTemplateDto = _mapper.Map<DesignTemplateDTO>(designTemplate);

        return designTemplateDto;
    }
}
