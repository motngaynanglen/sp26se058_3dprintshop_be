using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Transactions.Queries;

public record GetTransactionByOrderIdQuery : IRequest<TransactionDTO>
{
    public Guid OrderId { get; init; }
}

public class GetTransactionByOrderIdQueryHandler : IRequestHandler<GetTransactionByOrderIdQuery, TransactionDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetTransactionByOrderIdQueryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<TransactionDTO> Handle(GetTransactionByOrderIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Include(t => t.Invoice)
                .ThenInclude(i => i.Order)
            .Where(t => t.Invoice.OrderId == request.OrderId);

        if (_user.Role == Roles.CUSTOMER)
        {
            var accountId = _user.Id.ToGuid();
            var customerId = await _context.Customers
                .Where(c => c.AccountId == accountId)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (customerId == Guid.Empty)
                throw new UnauthorizedAccessException("Không xác định được khách hàng.");

            query = query.Where(t => t.Invoice.Order.CustomerId == customerId);
        }

        var entity = await query
            .OrderByDescending(t => t.Created)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy giao dịch cho đơn {request.OrderId}.");

        return _mapper.Map<TransactionDTO>(entity);
    }
}
