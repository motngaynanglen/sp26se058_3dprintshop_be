using System.ComponentModel;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

[Authorize(Roles = Roles.MANAGER)]
public record DeleteMaterialCommand : IRequest<bool>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid Id { get; init; }
}

public class DeleteMaterialCommandHandler : IRequestHandler<DeleteMaterialCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteMaterialCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<bool> Handle(DeleteMaterialCommand request, CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (material == null)
        {
            throw new DataNotFoundException(nameof(Material), request.Id);
        }

        var isUsedByPublishedVariant = await _context.DesignVariants.AnyAsync(x =>
            x.MaterialId == request.Id &&
            x.CatalogStatus == CatalogStatuses.Published &&
            x.IsActive,
            cancellationToken);

        if (isUsedByPublishedVariant)
        {
            throw new BusinessException("Không thể xóa vật liệu đang được dùng bởi biến thể sản phẩm đang mở bán. Hãy ngưng bán biến thể hoặc tạm ngưng vật liệu trước.");
        }

        var isUsedByTechnicalDraft = await _context.TechnicalDrafts.AnyAsync(x =>
            x.MaterialId == request.Id,
            cancellationToken);

        if (isUsedByTechnicalDraft)
        {
            throw new BusinessException("Không thể xóa vật liệu đã được dùng trong bản nháp kỹ thuật. Hãy tạm ngưng hoạt động vật liệu thay vì xóa.");
        }

        material.IsActive = false;
        material.Deleted = CoreHelper.SystemTimeNow;
        material.DeletedBy = _user.Username;
        material.LastModified = CoreHelper.SystemTimeNow;
        material.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
