using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignLogs.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignLogs.Commands;

[Authorize(Roles = Roles.CustomerStaffManager)]
public record CreateDesignLogCommand : IRequest<DesignLogDTO>
{
    //[DefaultValue(000)]
    public Guid DesignWorkId { get; init; }
    public Guid? ParentLogId { get; init; }
    [DefaultValue("Nội dung log")]
    public string? Content { get; init; }
    [DefaultValue("[\"https://f005.backblazeb2.com/file/3dprintshop/models/62b1ae312d6a48b69972d17e8058fe4c.glb\"]")]
    public List<string>? ImageUrls { get; init; }
    [DefaultValue("COMMUNICATION hoặc INTERNAL_NOTE")]
    public string LogType { get; init; } = "COMMUNICATION";
}

public class CreateDesignLogCommandValidator : AbstractValidator<CreateDesignLogCommand>
{
    public CreateDesignLogCommandValidator()
    {
        RuleFor(x => x.DesignWorkId)
            .NotEmpty();

        RuleFor(x => x.LogType)
            .NotEmpty()
            .Must(x => DesignLogType.All.Any(t => t.Value == x))
            .WithMessage("Loại ghi chép không hợp lệ.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content) || (x.ImageUrls?.Any() ?? false))
            .WithMessage("Vui lòng nhập nội dung hoặc đính kèm hình ảnh.");
    }
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
        var designWork = await _context.DesignWorks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.DesignWorkId, cancellationToken);

        if (designWork == null)
        {
            throw new DataNotFoundException(nameof(DesignWork), request.DesignWorkId);
        }

        var isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;
        if (!isStaffOrManager)
        {
            var userId = _user.Id.ToGuid();
            var customerOwnsDesignWork = await _context.Customers
                .AsNoTracking()
                .AnyAsync(x => x.AccountId == userId && x.Id == designWork.CustomerId, cancellationToken);

            if (!customerOwnsDesignWork)
            {
                throw new ForbiddenAccessException();
            }

            if (request.LogType == DesignLogType.InternalNote)
            {
                throw new ForbiddenAccessException("Khách hàng không được tạo ghi chú nội bộ.");
            }
        }

        if (request.ParentLogId.HasValue)
        {
            var parentLogExists = await _context.DesignLogs
                .AnyAsync(x => x.Id == request.ParentLogId && x.DesignWorkId == request.DesignWorkId, cancellationToken);

            if (!parentLogExists)
            {
                throw new DataNotFoundException(nameof(DesignLog), request.ParentLogId.Value);
            }
        }

        var log = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = request.DesignWorkId,
            ParentLogId = request.ParentLogId,
            AccountId = _user.Id != null ? Guid.Parse(_user.Id) : null,
            Content = request.Content?.Trim(),
            LogType = request.LogType,
            IsAI = false,
            Metadata = request.ImageUrls != null ? JsonSerializer.Serialize(request.ImageUrls) : null,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username
        };

        _context.DesignLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.DesignLogs
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.VersionHistories)
            .Where(x => x.Id == log.Id)
            .ProjectTo<DesignLogDTO>(_mapper.ConfigurationProvider)
            .SingleAsync(cancellationToken);
    }
}
