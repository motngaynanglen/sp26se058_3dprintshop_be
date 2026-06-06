using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Application.InventoryTransactions.Queries;
public record GetInventoryTransactionsByReferenceQuery : IRequest<List<InventoryTransactionDTO>>
{
    public Guid ReferenceId { get; set; }
}

public class GetInventoryTransactionsByReferenceQueryHandler: IRequestHandler<GetInventoryTransactionsByReferenceQuery, List<InventoryTransactionDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetInventoryTransactionsByReferenceQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<InventoryTransactionDTO>> Handle(GetInventoryTransactionsByReferenceQuery request, CancellationToken ct)
    {
        return await _context.InventoryTransactions
            .AsNoTracking()
            .Include(x => x.DesignVariant)
            .Include(x => x.Staff)
            .Where(x => x.ReferenceId == request.ReferenceId)
            .OrderByDescending(x => x.Created)
            .ProjectTo<InventoryTransactionDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}
