using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Queries;

public record GetDesignWorkByOrderItemQuery : IRequest<DesignWorkDTO>
{
    [JsonIgnore]
    public Guid OrderItemId { get; init; }
}

public class GetDesignWorkByOrderItemQueryHandler : IRequestHandler<GetDesignWorkByOrderItemQuery, DesignWorkDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    public GetDesignWorkByOrderItemQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    public async Task<DesignWorkDTO> Handle(GetDesignWorkByOrderItemQuery request, CancellationToken cancellationToken)
    {
        var orderItem = await _context.OrderItems
            .AsNoTracking()
            .FirstOrDefaultAsync(oi => oi.Id == request.OrderItemId, cancellationToken);
        if (orderItem == null)
        {
            throw new DataNotFoundException(nameof(OrderItem), request.OrderItemId);
        }
        var designWork = await _context.DesignWorks
            .AsNoTracking()
            .FirstOrDefaultAsync(dw => dw.Id == orderItem.DesignWorkId, cancellationToken);
        if (designWork == null)
        {
            throw new DataNotFoundException(nameof(DesignWork), request.OrderItemId);
        }
        return _mapper.Map<DesignWorkDTO>(designWork);
    }
}
