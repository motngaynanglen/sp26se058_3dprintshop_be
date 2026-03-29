using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Commands;

public record UpdateDesignVariantQuantityCommand : IRequest<Guid>
{
    public Guid Id { get; set; }
    public int AdditionalQuantity { get; set; }
}

public class UpdateDesignVariantQuantityCommandHandler : IRequestHandler<UpdateDesignVariantQuantityCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public UpdateDesignVariantQuantityCommandHandler(IApplicationDbContext context)
    {
        _context = context; 
    }
    public async Task<Guid> Handle(UpdateDesignVariantQuantityCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.DesignVariants.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (entity == null)
            throw new Exception("Không tìm thấy biến thể thiết kế");

        int oldestQuantity = entity.StockQuantity;
        entity.StockQuantity = oldestQuantity + command.AdditionalQuantity;
        entity.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
