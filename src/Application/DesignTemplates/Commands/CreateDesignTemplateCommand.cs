using System;
using System.ComponentModel;
using MediatR;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record CreateDesignTemplateCommand : IRequest<DesignTemplateDTO>
{
    [DefaultValue("CODE_DEFAULT")]
    public string Code { get; init; } = null!;
    [DefaultValue("Product Name")]
    public string Name { get; init; } = null!;
    [DefaultValue("Description here")]
    public string Description { get; init; } = null!;
    [DefaultValue("https://example.com/file.stl")]
    public string FileUrl { get; init; } = null!;
    [DefaultValue("https://example.com/thumbnail.png")]
    public string ThumbnailUrl { get; init; } = null!;
    [DefaultValue(true)]
    public bool IsAcitve { get; init; } = true;
}

public class CreateDesignTemplateCommandHandler : IRequestHandler<CreateDesignTemplateCommand, DesignTemplateDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CreateDesignTemplateCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignTemplateDTO> Handle(CreateDesignTemplateCommand request, CancellationToken cancellationToken)
    {
        var isExist = await _context.DesignTemplates.AnyAsync(t => t.Code == request.Code, cancellationToken);
        if (isExist)
        {
            throw new DuplicateException(nameof(DesignTemplates), nameof(request.Code), request.Code);
        }
        var newDesignTemplate = new DesignTemplate
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            FileUrl = request.FileUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            IsActive = true,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
        };

        _context.DesignTemplates.Add(newDesignTemplate);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Nếu có lỗi lạ từ DB (ví dụ lỗi DB chết, lỗi kết nối)
            throw new CreateFailureException(nameof(DesignTemplate), ex.Message);
        }

        return _mapper.Map<DesignTemplateDTO>(newDesignTemplate);
    }
}
