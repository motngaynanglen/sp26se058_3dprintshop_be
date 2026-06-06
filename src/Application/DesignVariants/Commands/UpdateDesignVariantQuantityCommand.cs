using System;
using System.ComponentModel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignVariants.Queries;

namespace sp26se058_3dprintshop_be.Application.DesignVariants.Commands;

public record UpdateDesignVariantQuantityCommand : IRequest<DesignVariantDTO>   // ← Thay Guid bằng DTO
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; set; }
    public int AdditionalQuantity { get; set; }
}

public class UpdateDesignVariantQuantityCommandHandler : IRequestHandler<UpdateDesignVariantQuantityCommand, DesignVariantDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateDesignVariantQuantityCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<DesignVariantDTO> Handle(UpdateDesignVariantQuantityCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.DesignVariants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (entity == null)
            throw new Exception("Không tìm thấy biến thể thiết kế");

        entity.StockQuantity += command.AdditionalQuantity;
        entity.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<DesignVariantDTO>(entity);
        return dto;
    }
}
