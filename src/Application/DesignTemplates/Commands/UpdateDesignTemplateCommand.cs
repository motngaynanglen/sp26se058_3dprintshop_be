using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Exceptions; // Nếu bạn có NotFoundException
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;

[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record UpdateDesignTemplateCommand : IRequest<DesignTemplateDTO>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid Id { get; init; }

    [DefaultValue("CODE_DEFAULT")]
    public string? Code { get; init; }

    [DefaultValue("Product Name")]
    public string? Name { get; init; }

    [DefaultValue("Description here")]
    public string? Description { get; init; }

    [DefaultValue("https://example.com/file.stl")]
    public string? FileUrl { get; init; }

    [DefaultValue("https://example.com/thumbnail.png")]
    public string? ThumbnailUrl { get; init; }
}

public class UpdateDesignTemplateCommandValidator : AbstractValidator<UpdateDesignTemplateCommand>
{
    public UpdateDesignTemplateCommandValidator()
    {
        RuleFor(x => x.Code)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("Mã mẫu thiết kế không được để trống.");

        RuleFor(x => x.Name)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("Tên mẫu thiết kế không được để trống.");

        RuleFor(x => x.FileUrl)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("File thiết kế không được để trống.");
    }
}

public class UpdateDesignTemplateCommandHandler : IRequestHandler<UpdateDesignTemplateCommand, DesignTemplateDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public UpdateDesignTemplateCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignTemplateDTO> Handle(UpdateDesignTemplateCommand request, CancellationToken cancellationToken)
    {
        var designTemplate = await _context.DesignTemplates
            .FirstOrDefaultAsync(dt => dt.Id == request.Id, cancellationToken);

        if (designTemplate == null)
        {
            throw new DataNotFoundException(nameof(DesignTemplate), request.Id);
        }
        // Nếu thay đổi Code, phải đảm bảo Code mới chưa bị ai khác sử dụng
        if (!string.IsNullOrEmpty(request.Code) && designTemplate.Code != request.Code.Trim())
        {
            var code = request.Code.Trim();
            var isCodeExist = await _context.DesignTemplates
                .AnyAsync(dt => dt.Code == code && dt.Id != request.Id, cancellationToken);

            if (isCodeExist)
            {
                throw new DuplicateException(nameof(DesignTemplate), nameof(request.Code), request.Code);
            }
        }

        designTemplate.Code = !string.IsNullOrEmpty(request.Code) ? request.Code.Trim() : designTemplate.Code;
        // Cập nhật thông tin
        if (!string.IsNullOrEmpty(request.Name))
            designTemplate.Name = request.Name.Trim();

        if (!string.IsNullOrEmpty(request.Description))
            designTemplate.Description = request.Description;

        if (!string.IsNullOrEmpty(request.FileUrl))
            designTemplate.FileUrl = request.FileUrl.Trim();

        if (!string.IsNullOrEmpty(request.ThumbnailUrl))
            designTemplate.ThumbnailUrl = request.ThumbnailUrl;

        designTemplate.LastModified = CoreHelper.SystemTimeNow;
        designTemplate.LastModifiedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Nếu có lỗi lạ từ DB (ví dụ lỗi DB chết, lỗi kết nối)
            throw new UpdateFailureException(nameof(DesignTemplate), ex.Message);
        }

        return _mapper.Map<DesignTemplateDTO>(designTemplate);
    }
}
