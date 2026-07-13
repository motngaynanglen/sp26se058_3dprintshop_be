using System.ComponentModel;
using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

[Authorize(Roles = Roles.MANAGER)]
public record UpdateMaterialCommand : IRequest<MaterialDTO>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid Id { get; init; }

    [DefaultValue("PLA")]
    public string? Name { get; init; }

    [DefaultValue("Nhựa PLA phổ biến cho in 3D.")]
    public string? Description { get; init; }

    [DefaultValue(null)]
    public bool? IsActive { get; init; }
}

public class UpdateMaterialCommandValidator : AbstractValidator<UpdateMaterialCommand>
{
    public UpdateMaterialCommandValidator()
    {
        RuleFor(x => x.Name)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("Tên vật liệu không được để trống.")
            .MaximumLength(100)
            .When(x => x.Name != null)
            .WithMessage("Tên vật liệu không vượt quá 100 ký tự.");

        RuleFor(x => x.Description)
            .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
            .WithMessage("Mô tả vật liệu không được để trống.")
            .MaximumLength(1000)
            .When(x => x.Description != null)
            .WithMessage("Mô tả không được vượt quá 1000 ký tự.");
    }
}

public class UpdateMaterialCommandHandler : IRequestHandler<UpdateMaterialCommand, MaterialDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public UpdateMaterialCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<MaterialDTO> Handle(UpdateMaterialCommand request, CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .Include(m => m.PriceHistories)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (material == null)
        {
            throw new DataNotFoundException(nameof(Material), request.Id);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            var duplicated = await _context.Materials
                .AnyAsync(x => x.Id != request.Id && x.Name.ToLower() == name.ToLower(), cancellationToken);

            if (duplicated)
            {
                throw new DuplicateException(nameof(Material), nameof(request.Name), request.Name);
            }

            material.Name = name;
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            material.Description = request.Description.Trim();
        }

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value && !material.PriceHistories.Any(x => x.IsCurrent))
            {
                throw new BusinessException("Không thể kích hoạt vật liệu khi chưa có giá hiện hành.");
            }

            material.IsActive = request.IsActive.Value;
        }

        material.LastModified = CoreHelper.SystemTimeNow;
        material.LastModifiedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException(nameof(Material), ex.Message);
        }

        return _mapper.Map<MaterialDTO>(material);
    }
}
