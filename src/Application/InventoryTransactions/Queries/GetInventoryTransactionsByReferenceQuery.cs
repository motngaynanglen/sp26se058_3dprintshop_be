using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.InventoryTransactions.Queries;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record GetInventoryTransactionsByReferenceQuery : IRequest<List<InventoryTransactionDTO>>
{
    public Guid ReferenceId { get; init; }
}
public class GetInventoryTransactionsByReferenceQueryValidator : AbstractValidator<GetInventoryTransactionsByReferenceQuery>
{
    public GetInventoryTransactionsByReferenceQueryValidator()
    {
        RuleFor(x => x.ReferenceId).NotEmpty().WithMessage("Mã tham chiếu không được để trống.");
    }
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
        var transactions = await _context.InventoryTransactions
            .AsNoTracking()
            .Where(x => x.ReferenceId == request.ReferenceId)
            .OrderByDescending(x => x.Created)
            .ProjectTo<InventoryTransactionDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return transactions;
    }
}
