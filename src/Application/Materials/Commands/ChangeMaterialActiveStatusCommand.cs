using System.ComponentModel;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

[Authorize(Roles = Roles.MANAGER)]
public record ActiveMaterialCommand(Guid Id) : IRequest<MaterialStatusResult>;

[Authorize(Roles = Roles.MANAGER)]
public record DeactiveMaterialCommand(Guid Id) : IRequest<MaterialStatusResult>;

[Authorize(Roles = Roles.MANAGER)]
public record ToggleMaterialActiveCommand : IRequest<MaterialStatusResult>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid Id { get; init; }
}

public record MaterialStatusResult(Guid Id, bool IsActive);

public class ActiveMaterialCommandHandler : IRequestHandler<ActiveMaterialCommand, MaterialStatusResult>
{
    private readonly MaterialActiveStatusService _service;

    public ActiveMaterialCommandHandler(MaterialActiveStatusService service)
    {
        _service = service;
    }

    public Task<MaterialStatusResult> Handle(ActiveMaterialCommand request, CancellationToken cancellationToken)
        => _service.SetActiveAsync(request.Id, true, cancellationToken);
}

public class DeactiveMaterialCommandHandler : IRequestHandler<DeactiveMaterialCommand, MaterialStatusResult>
{
    private readonly MaterialActiveStatusService _service;

    public DeactiveMaterialCommandHandler(MaterialActiveStatusService service)
    {
        _service = service;
    }

    public Task<MaterialStatusResult> Handle(DeactiveMaterialCommand request, CancellationToken cancellationToken)
        => _service.SetActiveAsync(request.Id, false, cancellationToken);
}

public class ToggleMaterialActiveCommandHandler : IRequestHandler<ToggleMaterialActiveCommand, MaterialStatusResult>
{
    private readonly IApplicationDbContext _context;
    private readonly MaterialActiveStatusService _service;

    public ToggleMaterialActiveCommandHandler(IApplicationDbContext context, MaterialActiveStatusService service)
    {
        _context = context;
        _service = service;
    }

    public async Task<MaterialStatusResult> Handle(ToggleMaterialActiveCommand request, CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (material == null)
        {
            throw new DataNotFoundException(nameof(Material), request.Id);
        }

        return await _service.SetActiveAsync(request.Id, !material.IsActive, cancellationToken);
    }
}

public class MaterialActiveStatusService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public MaterialActiveStatusService(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<MaterialStatusResult> SetActiveAsync(Guid materialId, bool isActive, CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .Include(x => x.PriceHistories)
            .FirstOrDefaultAsync(x => x.Id == materialId, cancellationToken);

        if (material == null)
        {
            throw new DataNotFoundException(nameof(Material), materialId);
        }

        if (isActive && !material.PriceHistories.Any(x => x.IsCurrent))
        {
            throw new BusinessException("Không thể kích hoạt vật liệu khi chưa có giá hiện hành.");
        }

        // Vật liệu có thể deactivate dù có sản phẩm đã bán.
        // Sau khi deactivate, vật liệu không hiện trong tìm kiếm khi tạo variant mới,
        // nhưng các variant cũ vẫn giữ nguyên materialId.

        material.IsActive = isActive;
        material.LastModified = CoreHelper.SystemTimeNow;
        material.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return new MaterialStatusResult(material.Id, material.IsActive);
    }
}
